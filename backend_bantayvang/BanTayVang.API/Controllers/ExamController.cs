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
        public async Task<ActionResult<BaseResponseDto<List<DethiDto>>>> GetAllExams([FromQuery] string? trangThai = null)
        {
            // DeptManager: auto-scope to their KhoaPhong
            string? khoaPhong = null;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                khoaPhong = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(khoaPhong))
                {
                    return BadRequest(BaseResponseDto<List<DethiDto>>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
            }

            var result = await _examService.GetAllExamsAsync(trangThai, khoaPhong);
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<ActionResult<BaseResponseDto<List<DethiDto>>>> GetActiveExams()
        {
            var result = await _examService.GetActiveExamsAsync();
            return Ok(result);
        }

        [HttpGet("code/{maDeThi}")]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> GetExamByCode(string maDeThi)
        {
            var result = await _examService.GetExamByCodeAsync(maDeThi);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> CreateExam([FromBody] CreateDethiDto createDto)
        {
            var nguoiTao = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                createDto.KhoaPhong = myKhoa;

                if (createDto.KyThiId.HasValue)
                {
                    var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                    var targetKyThi = await _context.KyThis.FindAsync(createDto.KyThiId.Value);
                    if (targetKyThi == null || targetKyThi.KhoaPhongId != myKhoaId)
                    {
                        return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (createDto.DanhSachIdCauHoi != null && createDto.DanhSachIdCauHoi.Any())
                {
                    var invalidQuestionsExist = await _context.Cauhois
                        .AnyAsync(q => createDto.DanhSachIdCauHoi.Contains(q.Id) && q.KhoaPhong != myKhoa && q.KhoaPhong != "Không thuộc ngân hàng");
                    if (invalidQuestionsExist)
                    {
                        return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tất cả câu hỏi trong đề thi phải thuộc về khoa của người quản lý hoặc câu hỏi tải lên dùng một lần."));
                    }
                }
            }

            var result = await _examService.CreateExamAsync(createDto, nguoiTao);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetExamByCode), new { maDeThi = createDto.MaDeThi }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> UpdateExam(int id, [FromBody] UpdateDethiDto updateDto)
        {
            var nguoiCapNhat = GetCurrentUserIdOrDefault();
            if (id != updateDto.Id)
            {
                return BadRequest(BaseResponseDto<DethiDto>.FailureResult("ID mismatch"));
            }

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.Dethis.FindAsync(id);
                if (existing == null || existing.KhoaPhong != myKhoa)
                {
                    return Forbid();
                }

                if (updateDto.KyThiId.HasValue)
                {
                    var myKhoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                    var targetKyThi = await _context.KyThis.FindAsync(updateDto.KyThiId.Value);
                    if (targetKyThi == null || targetKyThi.KhoaPhongId != myKhoaId)
                    {
                        return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Không thể liên kết đề thi với kỳ thi của khoa khác."));
                    }
                }

                if (updateDto.DanhSachIdCauHoi != null && updateDto.DanhSachIdCauHoi.Any())
                {
                    var invalidQuestionsExist = await _context.Cauhois
                        .AnyAsync(q => updateDto.DanhSachIdCauHoi.Contains(q.Id) && q.KhoaPhong != myKhoa && q.KhoaPhong != "Không thuộc ngân hàng");
                    if (invalidQuestionsExist)
                    {
                        return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tất cả câu hỏi trong đề thi phải thuộc về khoa của người quản lý hoặc câu hỏi tải lên dùng một lần."));
                    }
                }
            }

            var result = await _examService.UpdateExamAsync(id, updateDto, nguoiCapNhat);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPatch("{examId}/status")]
        [HttpPost("{examId}/status")]
        [HttpPut("{examId}/status")]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> UpdateExamStatus(int examId, [FromBody] UpdateExamStatusDto dto)
        {
            var nguoiCapNhat = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.Dethis.FindAsync(examId);
                if (existing == null || existing.KhoaPhong != myKhoa)
                {
                    return Forbid();
                }
            }

            var result = await _examService.UpdateExamStatusAsync(examId, dto.TrangThai, nguoiCapNhat);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("status")]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> UpdateExamStatusByBody([FromBody] UpdateExamStatusRequestDto dto)
        {
            var nguoiCapNhat = GetCurrentUserIdOrDefault();

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<DethiDto>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                var existing = await _context.Dethis.FindAsync(dto.ExamId);
                if (existing == null || existing.KhoaPhong != myKhoa)
                {
                    return Forbid();
                }
            }

            var result = await _examService.UpdateExamStatusAsync(dto.ExamId, dto.TrangThai, nguoiCapNhat);
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
                var existing = await _context.Dethis.FindAsync(examId);
                if (existing == null || existing.KhoaPhong != myKhoa)
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
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> StartExam([FromBody] StartExamDto startDto)
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

            if (dto.IdLuaChonDaChon == null || !dto.IdLuaChonDaChon.Any())
            {
                // No choices - save as text answer or empty
                var answerDto = new SubmitAnswerDto
                {
                    IdBaiThi = dto.IdBaiThi,
                    IdCauHoi = dto.IdCauHoi,
                    IdLuaChonDaChon = null,
                    CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                    DaLuu = dto.DaLuu
                };
                lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
            }
            else
            {
                foreach (var choiceId in dto.IdLuaChonDaChon)
                {
                    var answerDto = new SubmitAnswerDto
                    {
                        IdBaiThi = dto.IdBaiThi,
                        IdCauHoi = dto.IdCauHoi,
                        IdLuaChonDaChon = choiceId,
                        CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                        DaLuu = dto.DaLuu
                    };
                    lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
                }
            }

            return Ok(lastResult ?? new BaseResponseDto { Success = true, Message = "Lưu đáp án" });
        }

        [HttpGet("{baithiId}/progress")]
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> GetExamProgress(int baithiId)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.GetExamProgressAsync(baithiId, taikhoanId);
            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> SubmitExam([FromBody] SubmitExamDto submitDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();

            var result = await _examService.SubmitExamAsync(submitDto, taikhoanId);
            return Ok(result);
        }

        [HttpPost("warning")]
        public async Task<ActionResult<BaseResponseDto>> LogCheatingWarning([FromBody] CheatingWarningDto warningDto)
        {
            var result = await _examService.LogSuspiciousActivityAsync(
                warningDto.IdBaiThi,
                warningDto.LoaiCanhBao,
                warningDto.MoTa ?? "");
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
        public async Task<ActionResult<BaseResponseDto<List<BaithiDto>>>> GetMyResults()
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
                var existing = await _context.Dethis.FindAsync(id);
                if (existing == null || existing.KhoaPhong != myKhoa)
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
                var existing = await _context.Dethis.FindAsync(id);
                if (existing == null || existing.KhoaPhong != myKhoa)
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
            sb.AppendLine("<title>Đề thi: " + System.Net.WebUtility.HtmlEncode(exam.TenDeThi ?? "") + "</title>");
            sb.AppendLine("<style>body{font-family:Arial,sans-serif;max-width:800px;margin:0 auto;padding:20px;font-size:13px}");
            sb.AppendLine("h1{text-align:center;font-size:16px}.header{text-align:center;margin-bottom:20px}");
            sb.AppendLine(".question{margin:12px 0}.choice{margin:4px 0 4px 20px}.correct{font-weight:bold;color:#16a34a}");
            sb.AppendLine("@media print{.no-print{display:none}}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine($"<div class='header'><h1>ĐỀ THI: {System.Net.WebUtility.HtmlEncode(exam.TenDeThi ?? "")}</h1>");
            sb.AppendLine($"<p>Mã đề: <strong>{exam.MaDeThi}</strong> &nbsp;|&nbsp; Thời gian: <strong>{exam.ThoiGianLamBai} phút</strong> &nbsp;|&nbsp; Số câu: <strong>{exam.CauHois?.Count ?? 0}</strong></p>");
            sb.AppendLine($"<p>Khoa: {System.Net.WebUtility.HtmlEncode(exam.KhoaPhong ?? "—")}</p></div>");
            sb.AppendLine("<button class='no-print' onclick='window.print()' style='padding:8px 16px;margin-bottom:12px;cursor:pointer'>🖨 In đề thi</button>");
            sb.AppendLine("<hr/>");

            int stt = 1;
            foreach (var q in exam.CauHois ?? new())
            {
                sb.AppendLine($"<div class='question'><strong>Câu {stt++}:</strong> {System.Net.WebUtility.HtmlEncode(q.NoiDung ?? "")}");
                char letter = 'A';
                foreach (var c in q.Luachons ?? new())
                {
                    var cls = c.LaDapAnDung == true ? "choice correct" : "choice";
                    sb.AppendLine($"<div class='{cls}'>{letter++}. {System.Net.WebUtility.HtmlEncode(c.NoiDung ?? "")} {(c.LaDapAnDung == true ? "✓" : "")}</div>");
                }
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

    }
}
