using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Impl;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Grading & Result Reports controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GradingController : ControllerBase
    {
        private readonly IGradingService _gradingService;
        private readonly BanTayVangDbContext _db;
        private readonly ILogger<GradingController> _logger;

        public GradingController(IGradingService gradingService, BanTayVangDbContext db, ILogger<GradingController> logger)
        {
            _gradingService = gradingService;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// <summary>
        /// Lấy chi tiết kết quả bài thi (kèm câu trả lời)
        /// </summary>
        [HttpGet("result/{examSubmissionId}")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> GetResultDetail(int examSubmissionId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examSubmission = await _db.ExamSubmissions
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);
                
                if (examSubmission == null) return Forbid();
                
                bool isOwner = examSubmission.User?.Department == myDepartment ||
                               examSubmission.ExamPaper?.Department == myDepartment ||
                               examSubmission.ExamCampaign?.OrganizedBy == myDepartment;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.GetResultDetailAsync(examSubmissionId);
            if (!result.Success) return NotFound(result);

            if (result.Data != null)
            {
                var isStudent = User.IsInRole("Student") || (!DepartmentAuthHelper.IsAdmin(User) && !DepartmentAuthHelper.IsDeptManager(User));
                var currentUserId = HttpContext.Items["UserId"] as int?;

                if (isStudent)
                {
                    if (currentUserId == null || result.Data.UserId != currentUserId)
                    {
                        return Forbid();
                    }

                    if (!result.Data.IsResultPublished)
                    {
                        result.Data.TotalScore = null;
                        result.Data.CorrectAnswers = null;
                        result.Data.Answers = new List<AnswerDetailDto>();
                        result.Data.Pass = false;
                    }
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách kết quả của 1 đề thi (cho admin/teacher)
        /// </summary>
        [HttpGet("exam/{examId}/results")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetResultsByExam(int examId)
        {
            var result = await _gradingService.GetResultsByExamAsync(examId);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examPaper = await _db.ExamPapers.FindAsync(examId);
                bool isOwner = examPaper != null && examPaper.Department == myDepartment;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.Department == myDepartment).ToList();
                    }
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Bảng xếp hạng (top performers) của 1 đề thi
        /// </summary>
        [HttpGet("exam/{examId}/ranking")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetRanking(int examId, [FromQuery] int top = 50)
        {
            var result = await _gradingService.GetRankingByExamAsync(examId, top);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examPaper = await _db.ExamPapers.FindAsync(examId);
                bool isOwner = examPaper != null && examPaper.Department == myDepartment;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.Department == myDepartment).ToList();
                    }
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Chấm lại bài thi
        /// </summary>
        [HttpPost("regrade/{examSubmissionId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> Regrade(int examSubmissionId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examSubmission = await _db.ExamSubmissions
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);
                
                if (examSubmission == null) return Forbid();
                
                bool isOwner = examSubmission.User?.Department == myDepartment ||
                               examSubmission.ExamPaper?.Department == myDepartment ||
                               examSubmission.ExamCampaign?.OrganizedBy == myDepartment;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.RegradeAsync(examSubmissionId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/grading/pending-essay
        /// Trả về danh sách bài thi còn câu tự luận chưa được chấm (cho Admin & Quản lý Khoa)
        /// </summary>
        [HttpGet("pending-essay")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<object>>>> GetPendingEssay([FromQuery] bool isGraded = false)
        {
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;

            var pendingQuery = _db.ExamSubmissions
                .Where(b => b.Status == "Completed" || b.Status == "Submitted")
                .Include(b => b.User)
                .Include(b => b.ExamPaper)
                .Include(b => b.ExamCampaign)
                .AsQueryable();

            if (!isGraded)
            {
                pendingQuery = pendingQuery.Where(b => b.SubmissionDetails.Any(c =>
                    c.Question != null &&
                    c.Question.QuestionCategory != null &&
                    EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName) &&
                    c.ScoreObtained == null));
            }
            else
            {
                pendingQuery = pendingQuery.Where(b => 
                    b.SubmissionDetails.Any(c =>
                        c.Question != null &&
                        c.Question.QuestionCategory != null &&
                        EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName))
                    && 
                    !b.SubmissionDetails.Any(c =>
                        c.Question != null &&
                        c.Question.QuestionCategory != null &&
                        EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName) &&
                        c.ScoreObtained == null));
            }

            // Nếu là Quản lý Khoa thì lọc theo khoa của mình
            if (myDepartment != null)
            {
                pendingQuery = pendingQuery.Where(b =>
                    b.User!.Department == myDepartment ||
                    b.ExamPaper!.Department == myDepartment ||
                    b.ExamCampaign!.OrganizedBy == myDepartment);
            }

            var examSubmissions = await pendingQuery
                .OrderByDescending(b => b.SubmitTime)
                .Take(200)
                .Select(b => new
                {
                    ExamSubmissionId = b.Id,
                    UserId = b.UserId,
                    Username = b.User!.Username,
                    FullName = b.User.FullName,
                    EmployeeCode = b.User.EmployeeCode,
                    Department = b.User.Department,
                    ExamPaperCode = b.ExamPaperCode,
                    ExamPaperName = b.ExamPaper != null ? b.ExamPaper.ExamPaperName : null,
                    SubmitTime = b.SubmitTime,
                    TotalScore = b.TotalScore,
                    CorrectAnswers = b.CorrectAnswers,
                    TotalQuestions = b.TotalQuestions,
                    Status = b.Status,
                    // Đếm số câu tự luận chưa chấm
                    UngradedEssayQuestions = b.SubmissionDetails.Count(c =>
                        c.Question != null &&
                        c.Question.QuestionCategory != null &&
                        EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName) &&
                        c.ScoreObtained == null),
                    TotalEssayQuestions = b.SubmissionDetails.Count(c =>
                        c.Question != null &&
                        c.Question.QuestionCategory != null &&
                        EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName)),
                    CampaignName = b.ExamCampaign != null ? b.ExamCampaign.CampaignName : null,
                })
                .ToListAsync();

            return Ok(new BaseResponseDto<List<object>>
            {
                Success = true,
                Message = $"Tìm thấy {examSubmissions.Count} bài thi cần chấm tự luận",
                Data = examSubmissions.Cast<object>().ToList()
            });
        }

        [HttpGet("pending-essay-answers")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<object>>>> GetPendingEssayAnswers([FromQuery] int? examCampaignId = null, [FromQuery] int? examId = null, [FromQuery] bool ungradedOnly = false)
        {
            var isAdmin = DepartmentAuthHelper.IsAdmin(User);
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;

            var query = _db.SubmissionDetails
                .Include(c => c.Question)
                    .ThenInclude(q => q!.QuestionOptions)
                .Include(c => c.ExamSubmission)
                .ThenInclude(e => e.User)
                .Where(c => c.Question != null && c.Question.QuestionCategory != null &&
                            EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName));

            if (ungradedOnly)
            {
                query = query.Where(c => c.ScoreObtained == null);
            }

            if (examCampaignId.HasValue)
            {
                query = query.Where(c => c.ExamSubmission != null && c.ExamSubmission.ExamCampaignId == examCampaignId.Value);
            }

            if (examId.HasValue)
            {
                query = query.Where(c => c.ExamSubmission != null && c.ExamSubmission.ExamPaperId == examId.Value);
            }

            if (!isAdmin && myDepartment != null)
            {
                query = query.Where(c => c.ExamSubmission != null && c.ExamSubmission.User != null && c.ExamSubmission.User.Department == myDepartment);
            }

            var answers = await query
                .OrderByDescending(c => c.ExamSubmission!.SubmitTime)
                .Select(c => new
                {
                    SubmissionDetailId = c.Id,
                    ExamSubmissionId = c.ExamSubmissionId,
                    UserId = c.ExamSubmission!.UserId,
                    FullName = c.ExamSubmission.User!.FullName,
                    EmployeeCode = c.ExamSubmission.User.EmployeeCode,
                    // BUG FIX (confirmed live): a student who retakes an exam gets a brand new
                    // ExamSubmissionId per attempt, but nothing here told the grader WHICH attempt
                    // an essay answer belonged to - two cards for the same student (one per
                    // attempt) looked identical apart from the answer text itself. A grader
                    // reviewing the wrong one (e.g. an old blank/failed attempt) instead of the
                    // latest one wouldn't notice. SubmitTime lets the UI show that.
                    SubmitTime = c.ExamSubmission.SubmitTime,
                    QuestionContent = c.Question!.Content,
                    EssayAnswer = c.EssayAnswer,
                    SuggestedAnswer = c.Question.SuggestedAnswer,
                    CorrectAnswerContent = c.Question.QuestionOptions.FirstOrDefault(o => o.IsCorrect == true) != null ? c.Question.QuestionOptions.FirstOrDefault(o => o.IsCorrect == true)!.Content : null,
                    ScoreObtained = c.ScoreObtained,
                    TeacherComment = c.TeacherComment,
                    // AI Grading fields
                    AiScore = c.AiScore,
                    AiComment = c.AiComment,
                    AiGradingStatus = c.AiGradingStatus
                })
                .Take(500)
                .ToListAsync();

            return Ok(new BaseResponseDto<List<object>>
            {
                Success = true,
                Data = answers.Cast<object>().ToList()
            });
        }

        /// <summary>
        /// Chấm thủ công câu tự luận
        /// </summary>
        [HttpPost("manual-grade")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ManualGrade([FromBody] ManualGradingDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var detail = await _db.SubmissionDetails
                    .Include(c => c.ExamSubmission)
                        .ThenInclude(b => b.User)
                    .Include(c => c.ExamSubmission)
                        .ThenInclude(b => b.ExamPaper)
                    .Include(c => c.ExamSubmission)
                        .ThenInclude(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(c => c.Id == dto.SubmissionDetailId);
                
                if (detail?.ExamSubmission == null) return Forbid();
                
                var examSubmission = detail.ExamSubmission;
                bool isOwner = examSubmission.User?.Department == myDepartment ||
                               examSubmission.ExamPaper?.Department == myDepartment ||
                               examSubmission.ExamCampaign?.OrganizedBy == myDepartment;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.ManualGradeAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Auto-grade tất cả bài thi chưa chấm
        /// </summary>
        [HttpPost("auto-grade-all")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<int>>> AutoGradeAll()
        {
            var restrictToDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;
            var result = await _gradingService.AutoGradeAllAsync(restrictToDepartment);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt AI chấm hàng loạt câu tự luận (async - trả về ngay, Worker xử lý nền).
        /// POST /api/grading/ai-grade-batch
        /// Body: { "submissionDetailIds": [1, 2, 3] }
        /// </summary>
        [HttpPost("ai-grade-batch")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> AiGradeBatch(
            [FromBody] AiGradeBatchDto dto,
            [FromServices] AiGradingQueue queue)
        {
            if (dto.SubmissionDetailIds == null || dto.SubmissionDetailIds.Count == 0)
                return BadRequest(new BaseResponseDto { Success = false, Message = "Danh sách ID trống" });

            if (dto.SubmissionDetailIds.Count > 200)
                return BadRequest(new BaseResponseDto { Success = false, Message = "Tối đa 200 câu mỗi lần" });

            // Chỉ chấm các câu chưa có điểm (ScoreObtained == null)
            var detailsQuery = _db.SubmissionDetails
                .Include(c => c.ExamSubmission!).ThenInclude(b => b.User)
                .Include(c => c.ExamSubmission!).ThenInclude(b => b.ExamPaper)
                .Include(c => c.ExamSubmission!).ThenInclude(b => b.ExamCampaign)
                .Where(c => dto.SubmissionDetailIds.Contains(c.Id) && c.ScoreObtained == null)
                .AsQueryable();

            // BUG FIX: unlike every other grading endpoint (ManualGrade, GetResultsByExam, ...),
            // this one queued AI grading for whatever submissionDetailIds were posted with no
            // department check at all - confirmed live: a DeptManager for "Công xa" could queue
            // AI grading for a "Khoa Ngoại" student's answer. Apply the same 3-way ownership
            // filter used everywhere else so a DeptManager can only queue their own department's.
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                detailsQuery = detailsQuery.Where(c =>
                    c.ExamSubmission!.User!.Department == myDepartment ||
                    c.ExamSubmission.ExamPaper!.Department == myDepartment ||
                    c.ExamSubmission.ExamCampaign!.OrganizedBy == myDepartment);
            }

            var details = await detailsQuery.ToListAsync();

            if (!details.Any())
                return Ok(new BaseResponseDto { Success = true, Message = "Không có câu nào cần chấm (tất cả đã có điểm)" });

            // Cập nhật trạng thái → Pending để FE biết đang xử lý
            foreach (var d in details)
                d.AiGradingStatus = "Pending";
            await _db.SaveChangesAsync();

            // Đẩy vào Queue - Worker sẽ xử lý ngầm
            foreach (var id in details.Select(d => d.Id))
                await queue.EnqueueAsync(id, dto.AutoFinalize);

            return Ok(new BaseResponseDto
            {
                Success = true,
                Message = dto.AutoFinalize
                    ? $"Đã đưa {details.Count} câu vào hàng đợi AI chấm. Điểm sẽ được chốt tự động."
                    : $"Đã đưa {details.Count} câu vào hàng đợi AI chấm. AI sẽ đưa ra gợi ý, cần người chấm duyệt lại trước khi chốt điểm."
            });
        }

        /// <summary>
        /// Export kết quả của 1 đề thi ra Excel
        /// </summary>
        [HttpGet("exam/{examId}/export")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> ExportExamResults(int examId)
        {
            var result = await _gradingService.GetResultsByExamAsync(examId);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examPaper = await _db.ExamPapers.FindAsync(examId);
                bool isOwner = examPaper != null && examPaper.Department == myDepartment;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.Department == myDepartment).ToList();
                    }
                }
            }

            if (!result.Success || result.Data == null || !result.Data.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu để xuất" });

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Ket Qua Thi");

            ws.Cell(1, 1).Value = "STT";
            ws.Cell(1, 2).Value = "Username";
            ws.Cell(1, 3).Value = "Ho Ten";
            ws.Cell(1, 4).Value = "Ma Nhan Vien";
            ws.Cell(1, 5).Value = "Khoa Phong";
            ws.Cell(1, 6).Value = "Ma De Thi";
            ws.Cell(1, 7).Value = "Ten De Thi";
            ws.Cell(1, 8).Value = "Thoi Gian Bat Dau";
            ws.Cell(1, 9).Value = "Thoi Gian Nop";
            ws.Cell(1, 10).Value = "Thoi Gian Lam (phut)";
            ws.Cell(1, 11).Value = "So Cau Dung";
            ws.Cell(1, 12).Value = "Tong So Cau";
            ws.Cell(1, 13).Value = "Tong Diem";
            ws.Cell(1, 14).Value = "Trang Thai";
            ws.Cell(1, 15).Value = "Ket Qua";
            ws.Cell(1, 16).Value = "So Canh Bao";
            ws.Cell(1, 17).Value = "Cong Bo Diem";

            var headerRange = ws.Range(1, 1, 1, 17);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;

            int row = 2;
            int stt = 1;
            foreach (var r in result.Data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = r.Username;
                ws.Cell(row, 3).Value = r.FullName;
                ws.Cell(row, 4).Value = r.EmployeeCode;
                ws.Cell(row, 5).Value = r.Department;
                ws.Cell(row, 6).Value = r.ExamPaperCode;
                ws.Cell(row, 7).Value = r.ExamPaperName;
                ws.Cell(row, 8).Value = r.StartTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 9).Value = r.SubmitTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 10).Value = r.DurationMinutes ?? 0;
                ws.Cell(row, 11).Value = r.CorrectAnswers ?? 0;
                ws.Cell(row, 12).Value = r.TotalQuestions ?? 0;
                ws.Cell(row, 13).Value = r.TotalScore ?? 0;
                ws.Cell(row, 14).Value = r.Status;
                ws.Cell(row, 15).Value = r.Pass ? "Đạt" : "Không đạt";
                ws.Cell(row, 16).Value = r.WarningCount ?? 0;
                ws.Cell(row, 17).Value = r.IsResultPublished == true ? "Đã công bố" : "Chưa công bố";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"KetQua_DeThi_{examId}_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Export bảng xếp hạng ra Excel
        /// </summary>
        [HttpGet("exam/{examId}/ranking/export")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> ExportRanking(int examId, [FromQuery] int top = 50)
        {
            var result = await _gradingService.GetRankingByExamAsync(examId, top);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examPaper = await _db.ExamPapers.FindAsync(examId);
                bool isOwner = examPaper != null && examPaper.Department == myDepartment;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.Department == myDepartment).ToList();
                    }
                }
            }

            if (!result.Success || result.Data == null || !result.Data.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu để xuất" });

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Bang Xep Hang");

            ws.Cell(1, 1).Value = "Hang";
            ws.Cell(1, 2).Value = "Username";
            ws.Cell(1, 3).Value = "Ho Ten";
            ws.Cell(1, 4).Value = "Ma Nhan Vien";
            ws.Cell(1, 5).Value = "Khoa Phong";
            ws.Cell(1, 6).Value = "Tong Diem";
            ws.Cell(1, 7).Value = "So Cau Dung";
            ws.Cell(1, 8).Value = "Thoi Gian Lam (phut)";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Gold;

            int row = 2;
            int rank = 1;
            foreach (var r in result.Data)
            {
                ws.Cell(row, 1).Value = rank++;
                ws.Cell(row, 2).Value = r.Username;
                ws.Cell(row, 3).Value = r.FullName;
                ws.Cell(row, 4).Value = r.EmployeeCode;
                ws.Cell(row, 5).Value = r.Department;
                ws.Cell(row, 6).Value = r.TotalScore ?? 0;
                ws.Cell(row, 7).Value = r.CorrectAnswers ?? 0;
                ws.Cell(row, 8).Value = r.DurationMinutes ?? 0;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BangXepHang_DeThi_{examId}_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Export các bài thi được chọn ra Excel (kèm theo số lần thi tương ứng hiển thị trên UI)
        /// </summary>
        [HttpPost("export-selected")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> ExportSelected([FromBody] List<SelectedExportItemDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest(new { success = false, message = "Không có dữ liệu để xuất" });

            var ExamSubmissionIds = items.Select(i => i.ExamSubmissionId).ToList();

            var examSubmissions = await _db.ExamSubmissions
                .Include(b => b.User)
                .Include(b => b.ExamPaper)
                .Include(b => b.ExamCampaign)
                .Where(b => ExamSubmissionIds.Contains(b.Id))
                .ToListAsync();

            // Maintain the original order sent from frontend
            var orderedBaithis = items
                .Select(item => new {
                    Item = item,
                    ExamSubmission = examSubmissions.FirstOrDefault(b => b.Id == item.ExamSubmissionId)
                })
                .Where(x => x.ExamSubmission != null)
                .ToList();

            if (!orderedBaithis.Any())
                return NotFound(new { success = false, message = "Không tìm thấy dữ liệu bài thi để xuất" });

            // DeptManager check
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                orderedBaithis = orderedBaithis.Where(x => 
                    x.ExamSubmission.User?.Department == myDepartment ||
                    x.ExamSubmission.ExamPaper?.Department == myDepartment ||
                    x.ExamSubmission.ExamCampaign?.OrganizedBy == myDepartment
                ).ToList();
            }

            if (!orderedBaithis.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu hợp lệ để xuất" });

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Ket Qua Thi");

            ws.Cell(1, 1).Value = "STT";
            ws.Cell(1, 2).Value = "Username";
            ws.Cell(1, 3).Value = "Ho Ten";
            ws.Cell(1, 4).Value = "Ma Nhan Vien";
            ws.Cell(1, 5).Value = "Khoa Phong";
            ws.Cell(1, 6).Value = "Ma De Thi";
            ws.Cell(1, 7).Value = "Ten De Thi";
            ws.Cell(1, 8).Value = "Thoi Gian Bat Dau";
            ws.Cell(1, 9).Value = "Thoi Gian Nop";
            ws.Cell(1, 10).Value = "Thoi Gian Lam (phut)";
            ws.Cell(1, 11).Value = "So Cau Dung";
            ws.Cell(1, 12).Value = "Tong So Cau";
            ws.Cell(1, 13).Value = "Tong Diem";
            ws.Cell(1, 14).Value = "Trang Thai";
            ws.Cell(1, 15).Value = "Lần thi";
            ws.Cell(1, 16).Value = "Kết quả";
            ws.Cell(1, 17).Value = "So Canh Bao";
            ws.Cell(1, 18).Value = "Cong Bo Diem";

            var headerRange = ws.Range(1, 1, 1, 18);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;

            int row = 2;
            int stt = 1;
            foreach (var x in orderedBaithis)
            {
                var b = x.ExamSubmission;
                var duration = (b.SubmitTime.HasValue && b.StartTime.HasValue)
                    ? (int)(b.SubmitTime.Value - b.StartTime.Value).TotalMinutes : 0;

                var isPass = b.ExamCampaign?.MinPassQuestions != null 
                    ? (b.CorrectAnswers ?? 0) >= b.ExamCampaign.MinPassQuestions.Value 
                    : (b.ExamPaper?.MinPassQuestions != null 
                        ? (b.CorrectAnswers ?? 0) >= b.ExamPaper.MinPassQuestions.Value 
                        : true);

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = b.User?.Username ?? "";
                ws.Cell(row, 3).Value = b.User?.FullName ?? "";
                ws.Cell(row, 4).Value = b.User?.EmployeeCode ?? "";
                ws.Cell(row, 5).Value = b.User?.Department ?? "";
                ws.Cell(row, 6).Value = b.ExamPaperCode ?? b.ExamPaper?.ExamPaperCode ?? "";
                ws.Cell(row, 7).Value = b.ExamPaper?.ExamPaperName ?? "";
                ws.Cell(row, 8).Value = b.StartTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 9).Value = b.SubmitTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 10).Value = duration;
                ws.Cell(row, 11).Value = b.CorrectAnswers ?? 0;
                ws.Cell(row, 12).Value = b.TotalQuestions ?? 0;
                ws.Cell(row, 13).Value = b.CorrectAnswers ?? 0;
                ws.Cell(row, 14).Value = b.Status ?? "";
                ws.Cell(row, 15).Value = x.Item.AttemptNumber;
                ws.Cell(row, 16).Value = isPass ? "Đạt" : "Không đạt";
                ws.Cell(row, 17).Value = b.WarningCount ?? 0;
                ws.Cell(row, 18).Value = (b.IsIndividualResultPublished || (b.ExamPaper?.IsResultPublished ?? false)) ? "Đã công bố" : "Chưa công bố";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"KetQua_BaoCao_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Lấy kết quả thi phân cấp theo Kỳ thi (cho DeptManager & Admin)
        /// </summary>
        [HttpGet("by-exam-campaign/{examCampaignId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetByKyThi(int examCampaignId)
        {
            try
            {
                var result = await _gradingService.GetResultsByExamCampaignAsync(examCampaignId);

                if (DepartmentAuthHelper.IsDeptManager(User))
                {
                    var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                    var examCampaign = await _db.ExamCampaigns.FindAsync(examCampaignId);
                    bool isOwner = examCampaign != null && examCampaign.OrganizedBy == myDepartment;

                    if (!isOwner)
                    {
                        if (result.Success && result.Data != null)
                        {
                            result.Data = result.Data.Where(r => r.Department == myDepartment).ToList();
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kết quả thi theo kỳ thi {ExamCampaignId}", examCampaignId);
                return Ok(new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại.",
                    Data = new List<ExamResultDetailDto>()
                });
            }
        }

        /// <summary>
        /// PUT /api/grading/danh-gia/{examSubmissionId} — Quản lý khoa đánh giá thí sinh
        /// </summary>
        [HttpPut("evaluate/{examSubmissionId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> EvaluateCandidate(int examSubmissionId, [FromBody] DanhGiaThiSinhDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examSubmission = await _db.ExamSubmissions
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);
                
                if (examSubmission == null) return Forbid();
                
                bool isOwner = examSubmission.User?.Department == myDepartment ||
                               examSubmission.ExamPaper?.Department == myDepartment ||
                               examSubmission.ExamCampaign?.OrganizedBy == myDepartment;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.EvaluateCandidateAsync(examSubmissionId, dto.Evaluation);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // =====================================================================
        // TÍNH NĂNG MỚI: Công bố điểm cho từng thí sinh cụ thể
        // =====================================================================

        /// <summary>
        /// POST /api/grading/publish-single/{examSubmissionId}
        /// Admin hoặc Quản lý khoa công bố điểm cho 1 thí sinh cụ thể.
        /// Ghi nhận vào bảng ExamSubmission.IsIndividualResultPublished = true.
        /// </summary>
        [HttpPost("publish-single/{examSubmissionId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> PublishSingle(int examSubmissionId)
        {
            var examSubmission = await _db.ExamSubmissions
                .Include(b => b.ExamPaper)
                .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

            if (examSubmission == null)
                return NotFound(new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" });

            // BUG FIX: unlike ToggleExamVisibility and AutoPublishExpiredExamsJob (both of which
            // filter their "any ungraded essay" query to Status=="Completed" submissions only),
            // this endpoint never checked the submission's own Status at all. Calling it on an
            // InProgress submission with a pure-MCQ exam (no essay rows to match the query below)
            // would mark a not-yet-finished attempt as published while TotalScore/CorrectAnswers
            // are still null.
            if (examSubmission.Status != "Completed")
            {
                return BadRequest(new BaseResponseDto { Success = false, Message = "Chỉ có thể công bố điểm cho bài thi đã nộp" });
            }

            // DeptManager chỉ được công bố bài thi của khoa mình hoặc nếu quản lý đề thi/kỳ thi đó
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                
                var baithiFull = await _db.ExamSubmissions
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                bool isOwner = baithiFull?.User?.Department == myDepartment ||
                               baithiFull?.ExamPaper?.Department == myDepartment ||
                               baithiFull?.ExamCampaign?.OrganizedBy == myDepartment;
                               
                if (!isOwner) return Forbid();
            }

            examSubmission.IsIndividualResultPublished = true;
            examSubmission.IndividualPublishedAt = DateTime.UtcNow.AddHours(7);
            var nguoiCongBo = DepartmentAuthHelper.GetUserId(User);
            examSubmission.IndividualPublisherId = nguoiCongBo;

            var hasUngraded = await _db.SubmissionDetails
                .AnyAsync(c => c.ExamSubmissionId == examSubmissionId
                            && c.Question != null
                            && c.Question.QuestionCategory != null
                            && EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName)
                            && c.ScoreObtained == null);
            if (hasUngraded)
            {
                return BadRequest(new BaseResponseDto { Success = false, Message = "Không thể công bố điểm vì còn câu hỏi tự luận chưa được chấm." });
            }

            await _db.SaveChangesAsync();

            return Ok(new BaseResponseDto
            {
                Success = true,
                Message = $"Đã công bố điểm cho thí sinh (bài thi #{examSubmissionId})"
            });
        }

        /// <summary>
        /// POST /api/grading/unpublish-single/{examSubmissionId}
        /// Thu hồi công bố điểm của 1 thí sinh.
        /// </summary>
        [HttpPost("unpublish-single/{examSubmissionId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> UnpublishSingle(int examSubmissionId)
        {
            var examSubmission = await _db.ExamSubmissions
                .Include(b => b.ExamPaper)
                .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

            if (examSubmission == null)
                return NotFound(new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" });

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (examSubmission.ExamPaper?.Department != myDepartment)
                    return Forbid();
            }

            examSubmission.IsIndividualResultPublished = false;
            examSubmission.IndividualPublishedAt = null;
            examSubmission.IndividualPublisherId = null;

            await _db.SaveChangesAsync();

            return Ok(new BaseResponseDto
            {
                Success = true,
                Message = "Đã thu hồi công bố điểm"
            });
        }
    }
}

