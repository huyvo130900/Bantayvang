using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Grading & Result Reports controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GradingController : ControllerBase
    {
        private readonly IGradingService _gradingService;

        public GradingController(IGradingService gradingService)
        {
            _gradingService = gradingService;
        }

        /// <summary>
        /// Lấy chi tiết kết quả bài thi (kèm câu trả lời)
        /// </summary>
        [HttpGet("result/{baiThiId}")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> GetResultDetail(int baiThiId)
        {
            var result = await _gradingService.GetResultDetailAsync(baiThiId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách kết quả của 1 đề thi (cho admin/teacher)
        /// </summary>
        [HttpGet("exam/{examId}/results")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetResultsByExam(int examId)
        {
            var result = await _gradingService.GetResultsByExamAsync(examId);
            return Ok(result);
        }

        /// <summary>
        /// Bảng xếp hạng (top performers) của 1 đề thi
        /// </summary>
        [HttpGet("exam/{examId}/ranking")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetRanking(int examId, [FromQuery] int top = 50)
        {
            var result = await _gradingService.GetRankingByExamAsync(examId, top);
            return Ok(result);
        }

        /// <summary>
        /// Chấm lại bài thi
        /// </summary>
        [HttpPost("regrade/{baiThiId}")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> Regrade(int baiThiId)
        {
            var result = await _gradingService.RegradeAsync(baiThiId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Chấm thủ công câu tự luận
        /// </summary>
        [HttpPost("manual-grade")]
        public async Task<ActionResult<BaseResponseDto>> ManualGrade([FromBody] ManualGradingDto dto)
        {
            var result = await _gradingService.ManualGradeAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Auto-grade tất cả bài thi chưa chấm
        /// </summary>
        [HttpPost("auto-grade-all")]
        public async Task<ActionResult<BaseResponseDto<int>>> AutoGradeAll()
        {
            var result = await _gradingService.AutoGradeAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Export kết quả của 1 đề thi ra Excel
        /// </summary>
        [HttpGet("exam/{examId}/export")]
        public async Task<IActionResult> ExportExamResults(int examId)
        {
            var result = await _gradingService.GetResultsByExamAsync(examId);
            if (!result.Success || result.Data == null || !result.Data.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu để xuất" });

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Ket Qua Thi");

            // Header
            ws.Cell(1, 1).Value = "STT";
            ws.Cell(1, 2).Value = "Username";
            ws.Cell(1, 3).Value = "Ho Ten";
            ws.Cell(1, 4).Value = "Khoa Phong";
            ws.Cell(1, 5).Value = "Ma De Thi";
            ws.Cell(1, 6).Value = "Ten De Thi";
            ws.Cell(1, 7).Value = "Thoi Gian Bat Dau";
            ws.Cell(1, 8).Value = "Thoi Gian Nop";
            ws.Cell(1, 9).Value = "Thoi Gian Lam (phut)";
            ws.Cell(1, 10).Value = "So Cau Dung";
            ws.Cell(1, 11).Value = "Tong So Cau";
            ws.Cell(1, 12).Value = "Tong Diem";
            ws.Cell(1, 13).Value = "Trang Thai";
            ws.Cell(1, 14).Value = "Ket Qua";
            ws.Cell(1, 15).Value = "So Canh Bao";

            var headerRange = ws.Range(1, 1, 1, 15);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;

            int row = 2;
            int stt = 1;
            foreach (var r in result.Data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = r.Username;
                ws.Cell(row, 3).Value = r.FullName;
                ws.Cell(row, 4).Value = r.KhoaPhong;
                ws.Cell(row, 5).Value = r.MaDeThi;
                ws.Cell(row, 6).Value = r.TenDeThi;
                ws.Cell(row, 7).Value = r.ThoiGianBatDau?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 8).Value = r.ThoiGianNop?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 9).Value = r.DurationMinutes ?? 0;
                ws.Cell(row, 10).Value = r.SoCauDung ?? 0;
                ws.Cell(row, 11).Value = r.TongSoCau ?? 0;
                ws.Cell(row, 12).Value = r.TongDiem ?? 0;
                ws.Cell(row, 13).Value = r.TrangThai;
                ws.Cell(row, 14).Value = r.Pass ? "Đạt" : "Không đạt";
                ws.Cell(row, 15).Value = r.SoCanhBao ?? 0;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"KetQua_DeThi_{examId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Export bảng xếp hạng ra Excel
        /// </summary>
        [HttpGet("exam/{examId}/ranking/export")]
        public async Task<IActionResult> ExportRanking(int examId, [FromQuery] int top = 50)
        {
            var result = await _gradingService.GetRankingByExamAsync(examId, top);
            if (!result.Success || result.Data == null || !result.Data.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu để xuất" });

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Bang Xep Hang");

            ws.Cell(1, 1).Value = "Hang";
            ws.Cell(1, 2).Value = "Username";
            ws.Cell(1, 3).Value = "Ho Ten";
            ws.Cell(1, 4).Value = "Khoa Phong";
            ws.Cell(1, 5).Value = "Tong Diem";
            ws.Cell(1, 6).Value = "So Cau Dung";
            ws.Cell(1, 7).Value = "Thoi Gian Lam (phut)";

            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Gold;

            int row = 2;
            int rank = 1;
            foreach (var r in result.Data)
            {
                ws.Cell(row, 1).Value = rank++;
                ws.Cell(row, 2).Value = r.Username;
                ws.Cell(row, 3).Value = r.FullName;
                ws.Cell(row, 4).Value = r.KhoaPhong;
                ws.Cell(row, 5).Value = r.TongDiem ?? 0;
                ws.Cell(row, 6).Value = r.SoCauDung ?? 0;
                ws.Cell(row, 7).Value = r.DurationMinutes ?? 0;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BangXepHang_DeThi_{examId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}