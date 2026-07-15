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
        public async Task<ActionResult<BaseResponseDto<List<ExamPaperDto>>>> GetAllExams([FromQuery] string? status = null)
        {
            // DeptManager: auto-scope to their Department
            string? department = null;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                department = DepartmentAuthHelper.GetKhoaPhong(User);
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
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> CreateExam([FromBody] CreateExamPaperDto createDto)
        {
            var createdBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                createDto.Department = myKhoa;

                if (createDto.KyThiId.HasValue)
                {
                    var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                    var targetKyThi = await _context.ExamCampaigns.FindAsync(createDto.KyThiId.Value);
                    if (targetKyThi == null || targetKyThi.DepartmentId != myKhoaId)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (createDto.DanhSachIdCauHoi != null && createDto.DanhSachIdCauHoi.Any())
                {
                    var invalidQuestionsExist = await _context.Questions
                        .AnyAsync(q => createDto.DanhSachIdCauHoi.Contains(q.Id) && q.Department != myKhoa && q.Department != "Không thuộc ngân hàng");
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
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExam(int id, [FromBody] UpdateExamPaperDto updateDto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();
            if (id != updateDto.Id)
            {
                return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("ID mismatch"));
            }

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myKhoa)
                {
                    return Forbid();
                }

                if (updateDto.KyThiId.HasValue)
                {
                    var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                    var targetKyThi = await _context.ExamCampaigns.FindAsync(updateDto.KyThiId.Value);
                    if (targetKyThi == null || targetKyThi.DepartmentId != myKhoaId)
                    {
                        return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (updateDto.DanhSachIdCauHoi != null && updateDto.DanhSachIdCauHoi.Any())
                {
                    var invalidQuestionsExist = await _context.Questions
                        .AnyAsync(q => updateDto.DanhSachIdCauHoi.Contains(q.Id) && q.Department != myKhoa && q.Department != "Không thuộc ngân hàng");
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
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExamStatus(int examId, [FromBody] UpdateExamStatusDto dto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(examId);
                if (existing == null || existing.Department != myKhoa)
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
        public async Task<ActionResult<BaseResponseDto<ExamPaperDto>>> UpdateExamStatusByBody([FromBody] UpdateExamStatusRequestDto dto)
        {
            var updatedBy = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<ExamPaperDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(dto.ExamId);
                if (existing == null || existing.Department != myKhoa)
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
        public async Task<ActionResult<BaseResponseDto>> DeleteExam(int examId)
        {
            var nguoiXoa = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(examId);
                if (existing == null || existing.Department != myKhoa)
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
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.StartExamAsync(startDto, taikhoanId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{baithiId}/questions")]
        public async Task<ActionResult<BaseResponseDto<List<ExamQuestionDto>>>> GetExamQuestions(int baithiId)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.GetExamQuestionsAsync(baithiId, taikhoanId);
            return Ok(result);
        }

        [HttpPost("answer")]
        public async Task<ActionResult<BaseResponseDto>> SaveAnswer([FromBody] SubmitAnswerDto answerDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
            return Ok(result);
        }

        /// <summary>
        /// Lưu nhiều đáp án cho 1 câu hỏi (Multiple correct answers)
        /// </summary>
        [HttpPost("answer-multiple")]
        public async Task<ActionResult<BaseResponseDto>> SaveMultipleAnswer([FromBody] SubmitMultipleAnswerDto dto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            // Save each choice as a separate answer
            BaseResponseDto? lastResult = null;

            if (dto.SelectedOptionId == null || !dto.SelectedOptionId.Any())
            {
                // No choices - save as text answer or empty
                var answerDto = new SubmitAnswerDto
                {
                    ExamSubmissionId = dto.ExamSubmissionId,
                    QuestionId = dto.QuestionId,
                    SelectedOptionId = null,
                    CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                    DaLuu = dto.DaLuu
                };
                lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
            }
            else
            {
                foreach (var choiceId in dto.SelectedOptionId)
                {
                    var answerDto = new SubmitAnswerDto
                    {
                        ExamSubmissionId = dto.ExamSubmissionId,
                        QuestionId = dto.QuestionId,
                        SelectedOptionId = choiceId,
                        CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                        DaLuu = dto.DaLuu
                    };
                    lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
                }
            }

            return Ok(lastResult ?? new BaseResponseDto { Success = true, Message = "Lưu đáp án" });
        }

        [HttpGet("{baithiId}/progress")]
        public async Task<ActionResult<BaseResponseDto<ExamSubmissionDto>>> GetExamProgress(int baithiId)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.GetExamProgressAsync(baithiId, taikhoanId);
            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<BaseResponseDto<ExamSubmissionDto>>> SubmitExam([FromBody] SubmitExamDto submitDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.SubmitExamAsync(submitDto, taikhoanId);
            return Ok(result);
        }

        [HttpPost("warning")]
        public async Task<ActionResult<BaseResponseDto>> LogCheatingWarning([FromBody] CheatingWarningDto warningDto)
        {
            var result = await _examService.LogSuspiciousActivityAsync(
                warningDto.ExamSubmissionId,
                warningDto.LoaiCanhBao,
                warningDto.Description ?? "");
            return Ok(result);
        }

        [HttpGet("{baithiId}/warnings")]
        public async Task<ActionResult<BaseResponseDto<int>>> GetWarningCount(int baithiId)
        {
            var result = await _examService.GetWarningCountAsync(baithiId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách bài thi đã nộp (Completed) của user hiện tại
        /// </summary>
        [HttpGet("my-results")]
        public async Task<ActionResult<BaseResponseDto<List<ExamSubmissionDto>>>> GetMyResults()
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            var result = await _examService.GetMyResultsAsync(taikhoanId);
            return Ok(result);
        }

        /// <summary>
        /// Get current user ID from JWT context, default to 1 (admin) if not authenticated
        /// </summary>
        private int GetCurrentUserIdOrDefault()
        {
            return HttpContext.Items["UserId"] as int? ?? 1;
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
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<ExamPreviewDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myKhoa)
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
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý.");
                }
                var existing = await _context.ExamPapers.FindAsync(id);
                if (existing == null || existing.Department != myKhoa)
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
