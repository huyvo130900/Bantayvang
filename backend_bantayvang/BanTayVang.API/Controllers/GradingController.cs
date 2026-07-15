using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
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

        public GradingController(IGradingService gradingService, BanTayVangDbContext db)
        {
            _gradingService = gradingService;
            _db = db;
        }

        /// <summary>
        /// <summary>
        /// Lấy chi tiết kết quả bài thi (kèm câu trả lời)
        /// </summary>
        [HttpGet("result/{baiThiId}")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> GetResultDetail(int baiThiId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var baithi = await _db.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);
                
                if (baithi == null) return Forbid();
                
                bool isOwner = baithi.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                               baithi.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                               baithi.KyThiNavigation?.DonViToChuc == myKhoa;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.GetResultDetailAsync(baiThiId);
            if (!result.Success) return NotFound(result);

            if (result.Data != null)
            {
                var isStudent = User.IsInRole("Student") || (!DepartmentAuthHelper.IsAdmin(User) && !DepartmentAuthHelper.IsDeptManager(User));
                var currentUserId = HttpContext.Items["UserId"] as int? ?? 1;

                if (isStudent && !result.Data.CongBoKetQua && result.Data.UserId == currentUserId)
                {
                    result.Data.TongDiem = null;
                    result.Data.SoCauDung = null;
                    result.Data.Answers = new List<AnswerDetailDto>();
                    result.Data.Pass = false;
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách kết quả của 1 đề thi (cho admin/teacher)
        /// </summary>
        [HttpGet("exam/{examId}/results")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetResultsByExam(int examId)
        {
            var result = await _gradingService.GetResultsByExamAsync(examId);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var dethi = await _db.Dethis.FindAsync(examId);
                bool isOwner = dethi != null && dethi.KhoaPhong == myKhoa;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.KhoaPhong == myKhoa).ToList();
                    }
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Bảng xếp hạng (top performers) của 1 đề thi
        /// </summary>
        [HttpGet("exam/{examId}/ranking")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetRanking(int examId, [FromQuery] int top = 50)
        {
            var result = await _gradingService.GetRankingByExamAsync(examId, top);

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var dethi = await _db.Dethis.FindAsync(examId);
                bool isOwner = dethi != null && dethi.KhoaPhong == myKhoa;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.KhoaPhong == myKhoa).ToList();
                    }
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Chấm lại bài thi
        /// </summary>
        [HttpPost("regrade/{baiThiId}")]
        public async Task<ActionResult<BaseResponseDto<ExamResultDetailDto>>> Regrade(int baiThiId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var baithi = await _db.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);
                
                if (baithi == null) return Forbid();
                
                bool isOwner = baithi.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                               baithi.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                               baithi.KyThiNavigation?.DonViToChuc == myKhoa;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.RegradeAsync(baiThiId);
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
            var myKhoa = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetKhoaPhong(User) : null;

            var pendingQuery = _db.Baithis
                .Where(b => b.TrangThai == "Completed" || b.TrangThai == "Submitted")
                .Include(b => b.IdTaiKhoanNavigation)
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.KyThiNavigation)
                .AsQueryable();

            if (!isGraded)
            {
                pendingQuery = pendingQuery.Where(b => b.Chitietlambais.Any(c =>
                    c.IdCauHoiNavigation != null &&
                    c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null &&
                    (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" ||
                     c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" ||
                     c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL") &&
                    c.DiemDatDuoc == null));
            }
            else
            {
                pendingQuery = pendingQuery.Where(b => 
                    b.Chitietlambais.Any(c =>
                        c.IdCauHoiNavigation != null &&
                        c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null &&
                        (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL"))
                    && 
                    !b.Chitietlambais.Any(c =>
                        c.IdCauHoiNavigation != null &&
                        c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null &&
                        (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL") &&
                        c.DiemDatDuoc == null));
            }

            // Nếu là Quản lý Khoa thì lọc theo khoa của mình
            if (myKhoa != null)
            {
                pendingQuery = pendingQuery.Where(b =>
                    b.IdTaiKhoanNavigation!.KhoaPhong == myKhoa ||
                    b.IdDeThiNavigation!.KhoaPhong == myKhoa ||
                    b.KyThiNavigation!.DonViToChuc == myKhoa);
            }

            var baithis = await pendingQuery
                .OrderByDescending(b => b.ThoiGianNop)
                .Take(200)
                .Select(b => new
                {
                    BaiThiId = b.Id,
                    UserId = b.IdTaiKhoan,
                    Username = b.IdTaiKhoanNavigation!.TenDangNhap,
                    FullName = b.IdTaiKhoanNavigation.HoTen,
                    MaNhanVien = b.IdTaiKhoanNavigation.MaNhanVien,
                    KhoaPhong = b.IdTaiKhoanNavigation.KhoaPhong,
                    MaDeThi = b.MaDeThi,
                    TenDeThi = b.IdDeThiNavigation != null ? b.IdDeThiNavigation.TenDeThi : null,
                    ThoiGianNop = b.ThoiGianNop,
                    TongDiem = b.TongDiem,
                    SoCauDung = b.SoCauDung,
                    TongSoCau = b.TongSoCau,
                    TrangThai = b.TrangThai,
                    // Đếm số câu tự luận chưa chấm
                    SoCauTuLuanChuaCham = b.Chitietlambais.Count(c =>
                        c.IdCauHoiNavigation != null &&
                        c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null &&
                        (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL") &&
                        c.DiemDatDuoc == null),
                    TongSoCauTuLuan = b.Chitietlambais.Count(c =>
                        c.IdCauHoiNavigation != null &&
                        c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null &&
                        (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" ||
                         c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL")),
                    TenKyThi = b.KyThiNavigation != null ? b.KyThiNavigation.TenKyThi : null,
                })
                .ToListAsync();

            return Ok(new BaseResponseDto<List<object>>
            {
                Success = true,
                Message = $"Tìm thấy {baithis.Count} bài thi cần chấm tự luận",
                Data = baithis.Cast<object>().ToList()
            });
        }

        /// <summary>
        /// Chấm thủ công câu tự luận
        /// </summary>
        [HttpPost("manual-grade")]
        public async Task<ActionResult<BaseResponseDto>> ManualGrade([FromBody] ManualGradingDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var detail = await _db.Chitietlambais
                    .Include(c => c.IdBaiThiNavigation)
                        .ThenInclude(b => b.IdTaiKhoanNavigation)
                    .Include(c => c.IdBaiThiNavigation)
                        .ThenInclude(b => b.IdDeThiNavigation)
                    .Include(c => c.IdBaiThiNavigation)
                        .ThenInclude(b => b.KyThiNavigation)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChiTietLamBaiId);
                
                if (detail?.IdBaiThiNavigation == null) return Forbid();
                
                var baithi = detail.IdBaiThiNavigation;
                bool isOwner = baithi.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                               baithi.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                               baithi.KyThiNavigation?.DonViToChuc == myKhoa;
                               
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

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var dethi = await _db.Dethis.FindAsync(examId);
                bool isOwner = dethi != null && dethi.KhoaPhong == myKhoa;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.KhoaPhong == myKhoa).ToList();
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
                ws.Cell(row, 4).Value = r.MaNhanVien;
                ws.Cell(row, 5).Value = r.KhoaPhong;
                ws.Cell(row, 6).Value = r.MaDeThi;
                ws.Cell(row, 7).Value = r.TenDeThi;
                ws.Cell(row, 8).Value = r.ThoiGianBatDau?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 9).Value = r.ThoiGianNop?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 10).Value = r.DurationMinutes ?? 0;
                ws.Cell(row, 11).Value = r.SoCauDung ?? 0;
                ws.Cell(row, 12).Value = r.TongSoCau ?? 0;
                ws.Cell(row, 13).Value = r.TongDiem ?? 0;
                ws.Cell(row, 14).Value = r.TrangThai;
                ws.Cell(row, 15).Value = r.Pass ? "Đạt" : "Không đạt";
                ws.Cell(row, 16).Value = r.SoCanhBao ?? 0;
                ws.Cell(row, 17).Value = r.CongBoKetQua == true ? "Đã công bố" : "Chưa công bố";
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

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var dethi = await _db.Dethis.FindAsync(examId);
                bool isOwner = dethi != null && dethi.KhoaPhong == myKhoa;
                
                if (!isOwner)
                {
                    if (result.Success && result.Data != null)
                    {
                        result.Data = result.Data.Where(r => r.KhoaPhong == myKhoa).ToList();
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
                ws.Cell(row, 4).Value = r.MaNhanVien;
                ws.Cell(row, 5).Value = r.KhoaPhong;
                ws.Cell(row, 6).Value = r.TongDiem ?? 0;
                ws.Cell(row, 7).Value = r.SoCauDung ?? 0;
                ws.Cell(row, 8).Value = r.DurationMinutes ?? 0;
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

        /// <summary>
        /// Export các bài thi được chọn ra Excel (kèm theo số lần thi tương ứng hiển thị trên UI)
        /// </summary>
        [HttpPost("export-selected")]
        public async Task<IActionResult> ExportSelected([FromBody] List<SelectedExportItemDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest(new { success = false, message = "Không có dữ liệu để xuất" });

            var baiThiIds = items.Select(i => i.BaiThiId).ToList();

            var baithis = await _db.Baithis
                .Include(b => b.IdTaiKhoanNavigation)
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.KyThiNavigation)
                .Where(b => baiThiIds.Contains(b.Id))
                .ToListAsync();

            // Maintain the original order sent from frontend
            var orderedBaithis = items
                .Select(item => new {
                    Item = item,
                    Baithi = baithis.FirstOrDefault(b => b.Id == item.BaiThiId)
                })
                .Where(x => x.Baithi != null)
                .ToList();

            if (!orderedBaithis.Any())
                return NotFound(new { success = false, message = "Không tìm thấy dữ liệu bài thi để xuất" });

            // DeptManager check
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                orderedBaithis = orderedBaithis.Where(x => 
                    x.Baithi.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                    x.Baithi.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                    x.Baithi.KyThiNavigation?.DonViToChuc == myKhoa
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
                var b = x.Baithi;
                var duration = (b.ThoiGianNop.HasValue && b.ThoiGianBatDau.HasValue)
                    ? (int)(b.ThoiGianNop.Value - b.ThoiGianBatDau.Value).TotalMinutes : 0;

                var isPass = b.KyThiNavigation?.SoCauDungToiThieu != null 
                    ? (b.SoCauDung ?? 0) >= b.KyThiNavigation.SoCauDungToiThieu.Value 
                    : (b.IdDeThiNavigation?.SoCauDungToiThieu != null 
                        ? (b.SoCauDung ?? 0) >= b.IdDeThiNavigation.SoCauDungToiThieu.Value 
                        : true);

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = b.IdTaiKhoanNavigation?.TenDangNhap ?? "";
                ws.Cell(row, 3).Value = b.IdTaiKhoanNavigation?.HoTen ?? "";
                ws.Cell(row, 4).Value = b.IdTaiKhoanNavigation?.MaNhanVien ?? "";
                ws.Cell(row, 5).Value = b.IdTaiKhoanNavigation?.KhoaPhong ?? "";
                ws.Cell(row, 6).Value = b.MaDeThi ?? b.IdDeThiNavigation?.MaDeThi ?? "";
                ws.Cell(row, 7).Value = b.IdDeThiNavigation?.TenDeThi ?? "";
                ws.Cell(row, 8).Value = b.ThoiGianBatDau?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 9).Value = b.ThoiGianNop?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 10).Value = duration;
                ws.Cell(row, 11).Value = b.SoCauDung ?? 0;
                ws.Cell(row, 12).Value = b.TongSoCau ?? 0;
                ws.Cell(row, 13).Value = b.SoCauDung ?? 0;
                ws.Cell(row, 14).Value = b.TrangThai ?? "";
                ws.Cell(row, 15).Value = x.Item.LanThi;
                ws.Cell(row, 16).Value = isPass ? "Đạt" : "Không đạt";
                ws.Cell(row, 17).Value = b.TongSoCanhBao ?? 0;
                ws.Cell(row, 18).Value = (b.CongBoRieng || (b.IdDeThiNavigation?.CongBoKetQua ?? false)) ? "Đã công bố" : "Chưa công bố";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"KetQua_BaoCao_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Lấy kết quả thi phân cấp theo Kỳ thi (cho DeptManager & Admin)
        /// </summary>
        [HttpGet("by-kythi/{kyThiId}")]
        public async Task<ActionResult<BaseResponseDto<List<ExamResultDetailDto>>>> GetByKyThi(int kyThiId)
        {
            try
            {
                var result = await _gradingService.GetResultsByKyThiAsync(kyThiId);

                if (DepartmentAuthHelper.IsDeptManager(User))
                {
                    var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                    var kythi = await _db.KyThis.FindAsync(kyThiId);
                    bool isOwner = kythi != null && kythi.DonViToChuc == myKhoa;

                    if (!isOwner)
                    {
                        if (result.Success && result.Data != null)
                        {
                            result.Data = result.Data.Where(r => r.KhoaPhong == myKhoa).ToList();
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception)
            {
                return Ok(new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = "Chưa có dữ liệu",
                    Data = new List<ExamResultDetailDto>()
                });
            }
        }

        /// <summary>
        /// PUT /api/grading/danh-gia/{baiThiId} — Quản lý khoa đánh giá thí sinh
        /// </summary>
        [HttpPut("danh-gia/{baiThiId}")]
        public async Task<IActionResult> DanhGiaThiSinh(int baiThiId, [FromBody] DanhGiaThiSinhDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                var baithi = await _db.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);
                
                if (baithi == null) return Forbid();
                
                bool isOwner = baithi.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                               baithi.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                               baithi.KyThiNavigation?.DonViToChuc == myKhoa;
                               
                if (!isOwner)
                {
                    return Forbid();
                }
            }

            var result = await _gradingService.DanhGiaThiSinhAsync(baiThiId, dto.DanhGia);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // =====================================================================
        // TÍNH NĂNG MỚI: Công bố điểm cho từng thí sinh cụ thể
        // =====================================================================

        /// <summary>
        /// POST /api/grading/publish-single/{baiThiId}
        /// Admin hoặc Quản lý khoa công bố điểm cho 1 thí sinh cụ thể.
        /// Ghi nhận vào bảng Baithi.CongBoRieng = true.
        /// </summary>
        [HttpPost("publish-single/{baiThiId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> PublishSingle(int baiThiId)
        {
            var baithi = await _db.Baithis
                .Include(b => b.IdDeThiNavigation)
                .FirstOrDefaultAsync(b => b.Id == baiThiId);

            if (baithi == null)
                return NotFound(new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" });

            // DeptManager chỉ được công bố bài thi của khoa mình hoặc nếu quản lý đề thi/kỳ thi đó
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                
                var baithiFull = await _db.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);

                bool isOwner = baithiFull?.IdTaiKhoanNavigation?.KhoaPhong == myKhoa ||
                               baithiFull?.IdDeThiNavigation?.KhoaPhong == myKhoa ||
                               baithiFull?.KyThiNavigation?.DonViToChuc == myKhoa;
                               
                if (!isOwner) return Forbid();
            }

            baithi.CongBoRieng = true;
            baithi.ThoiGianCongBoRieng = DateTime.Now;
            var nguoiCongBo = DepartmentAuthHelper.GetUserId(User);
            baithi.NguoiCongBoRieng = nguoiCongBo;

            var hasUngraded = await _db.Chitietlambais
                .AnyAsync(c => c.IdBaiThi == baiThiId
                            && c.IdCauHoiNavigation != null
                            && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null
                            && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan" || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TL")
                            && c.DiemDatDuoc == null);
            if (hasUngraded)
            {
                return BadRequest(new BaseResponseDto { Success = false, Message = "Không thể công bố điểm vì còn câu hỏi tự luận chưa được chấm." });
            }

            await _db.SaveChangesAsync();

            return Ok(new BaseResponseDto
            {
                Success = true,
                Message = $"Đã công bố điểm cho thí sinh (bài thi #{baiThiId})"
            });
        }

        /// <summary>
        /// POST /api/grading/unpublish-single/{baiThiId}
        /// Thu hồi công bố điểm của 1 thí sinh.
        /// </summary>
        [HttpPost("unpublish-single/{baiThiId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> UnpublishSingle(int baiThiId)
        {
            var baithi = await _db.Baithis
                .Include(b => b.IdDeThiNavigation)
                .FirstOrDefaultAsync(b => b.Id == baiThiId);

            if (baithi == null)
                return NotFound(new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" });

            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (baithi.IdDeThiNavigation?.KhoaPhong != myKhoa)
                    return Forbid();
            }

            baithi.CongBoRieng = false;
            baithi.ThoiGianCongBoRieng = null;
            baithi.NguoiCongBoRieng = null;

            await _db.SaveChangesAsync();

            return Ok(new BaseResponseDto
            {
                Success = true,
                Message = "Đã thu hồi công bố điểm"
            });
        }
    }
}

