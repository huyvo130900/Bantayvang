using BanTayVang.API.DTOs.AntiCheat;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanTayVang.API.Attributes;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Exam controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly ILogger<ExamController> _logger;
        private readonly BanTayVangDbContext _context;

        public ExamController(IExamService examService, ILogger<ExamController> logger, BanTayVangDbContext context)
        {
            _examService = examService;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamPaperDto>>>> GetAllExams([FromQuery] string? status = null)
        {
            // DeptManager: auto-scope to their Department
            string? department = null;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                department = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(department))
                {
                    return BadRequest(BaseResponseDto<List<ExamPaperDto>>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
            }

            var result = await _examService.GetAllExamsAsync(status, department);
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<ActionResult<BaseResponseDto<List<ExamPaperDto>>>> GetActiveExams()
        {
            var result = await _examService.GetActiveExamsAsync();
            return Ok(result);
        }

        [HttpGet("code/{examPaperCode}")]
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> GetExamByCode(string examPaperCode)
        {
            var result = await _examService.GetExamByCodeAsync(examPaperCode);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> CreateExam([FromBody] CreateExamPaperDto createDto)
        {
            var createdBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                createDto.Department = myDepartment;

                if (createDto.ExamCampaignId.HasValue)
                {
                    var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                    // BUG FIX: `targetExamCampaign.DepartmentId != myDepartmentId` is false when
                    // both are null - a DeptManager with no managed_department_id (myDepartmentId
                    // null) could still link an exam paper to any "shared" campaign
                    // (DepartmentId=null). myDepartmentId == null must fail closed on its own.
                    // A campaign can now be scoped to 1-n departments (ExamCampaignDepartments) -
                    // check membership there instead of the old single DepartmentId column.
                    var targetExamCampaignExists = await _context.ExamCampaigns.AnyAsync(k => k.Id == createDto.ExamCampaignId.Value);
                    var isLinkedToMyDept = myDepartmentId != null && await _context.Set<ExamCampaignDepartment>()
                        .AnyAsync(kd => kd.ExamCampaignId == createDto.ExamCampaignId.Value && kd.DepartmentId == myDepartmentId.Value);
                    if (!targetExamCampaignExists || myDepartmentId == null || !isLinkedToMyDept)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (createDto.QuestionIds != null && createDto.QuestionIds.Any())
                {
                    var invalidQuestionsExist = await _context.Questions
                        .AnyAsync(q => createDto.QuestionIds.Contains(q.Id) && q.Department != myDepartment && q.Department != "Không thuộc ngân hàng");
                    if (invalidQuestionsExist)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tất cả câu hỏi trong đề thi phải thuộc về khoa của người quản lý hoặc câu hỏi tải lên dùng một lần."));
                    }
                }
            }

            var result = await _examService.CreateExamAsync(createDto, createdBy);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetExamByCode), new { examPaperCode = createDto.ExamPaperCode }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExam(int id, [FromBody] UpdateExamPaperDto updateDto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();
            if (id != updateDto.Id)
            {
                return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("ID mismatch"));
            }

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }

                if (updateDto.ExamCampaignId.HasValue)
                {
                    var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                    // BUG FIX: same fail-open as CreateExam above - null == null must still be denied.
                    // Same ExamCampaignDepartments membership check as CreateExam above.
                    var targetExamCampaignExists = await _context.ExamCampaigns.AnyAsync(k => k.Id == updateDto.ExamCampaignId.Value);
                    var isLinkedToMyDept = myDepartmentId != null && await _context.Set<ExamCampaignDepartment>()
                        .AnyAsync(kd => kd.ExamCampaignId == updateDto.ExamCampaignId.Value && kd.DepartmentId == myDepartmentId.Value);
                    if (!targetExamCampaignExists || myDepartmentId == null || !isLinkedToMyDept)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (updateDto.QuestionIds != null && updateDto.QuestionIds.Any())
                {
                    var invalidQuestionsExist = await _context.Questions
                        .AnyAsync(q => updateDto.QuestionIds.Contains(q.Id) && q.Department != myDepartment && q.Department != "Không thuộc ngân hàng");
                    if (invalidQuestionsExist)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tất cả câu hỏi trong đề thi phải thuộc về khoa của người quản lý hoặc câu hỏi tải lên dùng một lần."));
                    }
                }
            }

            var result = await _examService.UpdateExamAsync(id, updateDto, updatedBy);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPatch("{examId}/status")]
        [HttpPost("{examId}/status")]
        [HttpPut("{examId}/status")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExamStatus(int examId, [FromBody] UpdateExamStatusDto dto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(examId);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _examService.UpdateExamStatusAsync(examId, dto.Status, updatedBy);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("status")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExamStatusByBody([FromBody] UpdateExamStatusRequestDto dto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(dto.ExamId);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _examService.UpdateExamStatusAsync(dto.ExamId, dto.Status, updatedBy);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{examId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteExam(int examId)
        {
            var nguoiXoa = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(examId);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _examService.DeleteExamAsync(examId, nguoiXoa);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("start")]
        public async Task<ActionResult<BaseResponseDto<ExamSubmissionDto>>> StartExam([FromBody] StartExamDto startDto)
        {
            var userId = GetCurrentUserIdOrDefault();

            var result = await _examService.StartExamAsync(startDto, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{examSubmissionId}/questions")]
        public async Task<ActionResult<BaseResponseDto<List<ExamQuestionDto>>>> GetExamQuestions(int examSubmissionId)
        {
            var userId = GetCurrentUserIdOrDefault();

            var result = await _examService.GetExamQuestionsAsync(examSubmissionId, userId);
            return Ok(result);
        }

        [HttpPost("answer")]
        public async Task<ActionResult<BaseResponseDto>> SaveAnswer([FromBody] SubmitAnswerDto answerDto)
        {
            var userId = GetCurrentUserIdOrDefault();

            var result = await _examService.SaveAnswerAsync(answerDto, userId);
            return Ok(result);
        }

        /// <summary>
        /// Lưu nhiều đáp án cho 1 câu hỏi (Multiple correct answers)
        /// </summary>
        [HttpPost("answer-multiple")]
        public async Task<ActionResult<BaseResponseDto>> SaveMultipleAnswer([FromBody] SubmitMultipleAnswerDto dto)
        {
            var userId = GetCurrentUserIdOrDefault();

            // OWASP A01: Broken Access Control - Verify ownership BEFORE mutating any data.
            // Without this, a caller could pass another user's ExamSubmissionId and the
            // RemoveRange below would delete that user's saved answers.
            var ownsSubmission = await _context.ExamSubmissions
                .AsNoTracking()
                .AnyAsync(b => b.Id == dto.ExamSubmissionId && b.UserId == userId);
            if (!ownsSubmission)
            {
                return Ok(new BaseResponseDto { Success = false, Message = "Không có quyền truy cập bài thi này" });
            }

            BaseResponseDto? lastResult = null;

            if (dto.SelectedOptionId == null || !dto.SelectedOptionId.Any())
            {
                // No choices selected – save as essay/empty answer (single row upsert)
                var answerDto = new SubmitAnswerDto
                {
                    ExamSubmissionId = dto.ExamSubmissionId,
                    QuestionId = dto.QuestionId,
                    SelectedOptionId = null,
                    EssayAnswer = dto.EssayAnswer,
                    EssayImageUrl = dto.EssayImageUrl,
                    IsSaved = dto.IsSaved
                };
                lastResult = await _examService.SaveAnswerAsync(answerDto, userId);
            }
            else
            {
                // [BUG FIX] For multi-choice questions, delete all previous answers first,
                // then insert one row per selected choice. Without this, toggling choices
                // would accumulate stale rows in SubmissionDetails and produce wrong scores.
                var existing = _context.SubmissionDetails
                    .Where(c => c.ExamSubmissionId == dto.ExamSubmissionId && c.QuestionId == dto.QuestionId);
                _context.SubmissionDetails.RemoveRange(existing);
                await _context.SaveChangesAsync();

                foreach (var choiceId in dto.SelectedOptionId)
                {
                    var answerDto = new SubmitAnswerDto
                    {
                        ExamSubmissionId = dto.ExamSubmissionId,
                        QuestionId = dto.QuestionId,
                        SelectedOptionId = choiceId,
                        EssayAnswer = dto.EssayAnswer,
                        EssayImageUrl = dto.EssayImageUrl,
                        IsSaved = dto.IsSaved
                    };
                    lastResult = await _examService.SaveAnswerAsync(answerDto, userId);
                }
            }

            return Ok(lastResult ?? new BaseResponseDto { Success = true, Message = "Lưu đáp án" });
        }

        [HttpGet("{examSubmissionId}/progress")]
        public async Task<ActionResult<BaseResponseDto<ExamSubmissionDto>>> GetExamProgress(int examSubmissionId)
        {
            var userId = GetCurrentUserIdOrDefault();

            var result = await _examService.GetExamProgressAsync(examSubmissionId, userId);
            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<BaseResponseDto<ExamSubmissionDto>>> SubmitExam([FromBody] SubmitExamDto submitDto)
        {
            var userId = GetCurrentUserIdOrDefault();

            var result = await _examService.SubmitExamAsync(submitDto, userId);
            return Ok(result);
        }

        [HttpGet("campaign/{examCampaignId}/monitor/active")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>>>> GetActiveMonitorList(int examCampaignId)
        {
            var myDeptId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
            var result = await _examService.GetActiveMonitorListAsync(examCampaignId, myDeptId, DepartmentAuthHelper.IsDeptManager(User));
            return Ok(result);
        }

        [HttpPost("{examSubmissionId}/force-submit")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ForceSubmit(int examSubmissionId)
        {
            var supervisorId = DepartmentAuthHelper.GetUserId(User);
            if (supervisorId == null)
                return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người thực hiện" });

            var myDeptId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
            var myDeptName = DepartmentAuthHelper.GetDepartmentClaim(User);
            var result = await _examService.ForceSubmitAsync(examSubmissionId, supervisorId.Value, myDeptId, myDeptName, DepartmentAuthHelper.IsDeptManager(User));
            return Ok(result);
        }

        [HttpPost("warning")]
        public async Task<ActionResult<BaseResponseDto>> LogCheatingWarning([FromBody] CheatingWarningDto warningDto)
        {
            var userId = GetCurrentUserIdOrDefault();
            var result = await _examService.LogSuspiciousActivityAsync(
                warningDto.ExamSubmissionId,
                userId,
                warningDto.WarningType,
                warningDto.Description ?? "");
            return Ok(result);
        }

        [HttpGet("{examSubmissionId}/warnings")]
        public async Task<ActionResult<BaseResponseDto<int>>> GetWarningCount(int examSubmissionId)
        {
            // BUG FIX: this endpoint had no ownership check at all - any authenticated user
            // (any student, any dept manager) could read the cheating-warning count of ANY
            // exam submission just by guessing/incrementing the id, leaking who got flagged for
            // cheating on someone else's exam. Restrict to: the submission's own student, an
            // Admin, or a DeptManager whose department the submission belongs to (same
            // ownership pattern already used in GradingController.GetResultDetail).
            if (!DepartmentAuthHelper.IsAdmin(User))
            {
                var examSubmission = await _context.ExamSubmissions
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                if (examSubmission == null) return NotFound(new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" });

                if (DepartmentAuthHelper.IsDeptManager(User))
                {
                    var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                    bool isOwnerDept = examSubmission.User?.Department == myDepartment ||
                                       examSubmission.ExamPaper?.Department == myDepartment ||
                                       examSubmission.ExamCampaign?.OrganizedBy == myDepartment;
                    if (!isOwnerDept) return Forbid();
                }
                else
                {
                    var currentUserId = DepartmentAuthHelper.GetUserId(User);
                    if (currentUserId == null || examSubmission.UserId != currentUserId) return Forbid();
                }
            }

            var result = await _examService.GetWarningCountAsync(examSubmissionId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách bài thi đã nộp (Completed) của user hiện tại
        /// </summary>
        [HttpGet("my-results")]
        public async Task<ActionResult<BaseResponseDto<List<ExamSubmissionDto>>>> GetMyResults()
        {
            var userId = GetCurrentUserIdOrDefault();
            var result = await _examService.GetMyResultsAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Get current user ID from JWT context, default to 1 (admin) if not authenticated
        /// </summary>
        private int GetCurrentUserIdOrDefault()
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong session");
            return userId.Value;
        }
        /// <summary>
        /// GET /api/Exam/{id}/preview — Xem trước đề thi với đầy đủ câu hỏi và đáp án (Admin + DeptManager)
        /// </summary>
        [HttpGet("{id}/preview")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamPreviewDto>>> PreviewExam(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<ExamPreviewDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _examService.GetExamPreviewAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/Exam/{id}/preview-print — HTML printable version của đề thi
        /// </summary>
        [HttpGet("{id}/preview-print")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> PreviewPrint(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý.");
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _examService.GetExamPreviewAsync(id);
            if (!result.Success) return NotFound(result.Message);

            var exam = result.Data!;
            var html = GeneratePrintHtml(exam);
            return Content(html, "text/html; charset=utf-8");
        }

        private static string GeneratePrintHtml(ExamPreviewDto exam)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang='vi'><head><meta charset='UTF-8'>");
            sb.AppendLine("<title>Đề thi: " + System.Net.WebUtility.HtmlEncode(exam.ExamPaperName ?? "") + "</title>");
            sb.AppendLine("<style>body{font-family:Arial,sans-serif;max-width:800px;margin:0 auto;padding:20px;font-size:13px}");
            sb.AppendLine(".print-header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:24px;border-bottom:2px solid #000;padding-bottom:12px}");
            sb.AppendLine(".hospital-brand{display:flex;align-items:center;gap:12px;width:48%}");
            sb.AppendLine(".hospital-logo{width:55px;height:55px;object-fit:contain;flex-shrink:0}");
            sb.AppendLine(".hospital-title{text-align:left;line-height:1.3}");
            sb.AppendLine(".hospital-title .line-parent{font-size:11px;font-weight:normal;text-transform:uppercase;color:#444;margin:0}");
            sb.AppendLine(".hospital-title .line-child{font-size:13px;font-weight:bold;text-transform:uppercase;color:#000;margin:0}");
            sb.AppendLine(".exam-info{width:48%;text-align:center;line-height:1.4}");
            sb.AppendLine(".exam-info h1{font-size:14px;font-weight:bold;margin:0 0 4px;text-transform:uppercase}");
            sb.AppendLine(".exam-info .meta{font-size:11px;color:#333}");
            sb.AppendLine(".question{margin:12px 0}.choice{margin:4px 0 4px 20px}.correct{font-weight:bold;color:#16a34a}");
            sb.AppendLine("@media print{.no-print{display:none}}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='print-header'>");
            sb.AppendLine("  <div class='hospital-brand'>");
            sb.AppendLine("    <img src='/logoBVND2.png' alt='Logo' class='hospital-logo' />");
            sb.AppendLine("    <div class='hospital-title'>");
            sb.AppendLine("      <div class='line-parent'>SỞ Y TẾ TP. HỒ CHÍ MINH</div>");
            sb.AppendLine("      <div class='line-child'>BỆNH VIỆN NHI ĐỒNG 2</div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='exam-info'>");
            sb.AppendLine($"    <h1>ĐỀ THI: {System.Net.WebUtility.HtmlEncode(exam.ExamPaperName ?? "")}</h1>");
            sb.AppendLine($"    <div class='meta'>Mã đề: <strong>{exam.ExamPaperCode}</strong> &nbsp;|&nbsp; Thời gian: <strong>{exam.DurationMinutes} phút</strong> &nbsp;|&nbsp; Số câu: <strong>{exam.Questions?.Count ?? 0}</strong>");
            sb.AppendLine($"    <br/>Khoa: <strong>{System.Net.WebUtility.HtmlEncode(exam.Department ?? "—")}</strong></div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<button class='no-print' onclick='window.print()' style='padding:8px 16px;margin-bottom:12px;cursor:pointer'>🖨 In đề thi</button>");
            sb.AppendLine("<hr/>");

            int stt = 1;
            foreach (var q in exam.Questions ?? new())
            {
                sb.AppendLine($"<div class='question'><strong>Câu {stt++}:</strong> {System.Net.WebUtility.HtmlEncode(q.Content ?? "")}");
                char letter = 'A';
                foreach (var c in q.QuestionOptions ?? new())
                {
                    var cls = c.IsCorrect == true ? "choice correct" : "choice";
                    sb.AppendLine($"<div class='{cls}'>{letter++}. {System.Net.WebUtility.HtmlEncode(c.Content ?? "")} {(c.IsCorrect == true ? "✓" : "")}</div>");
                }
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

    }
}
