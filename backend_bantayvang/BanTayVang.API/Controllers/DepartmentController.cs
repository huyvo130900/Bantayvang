using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Department;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ExcelDataReader;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Quản lý Khoa/Phòng ban - Admin only (trừ GET)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(BanTayVangDbContext context, ILogger<DepartmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET /api/Department - Danh sách khoa (Admin + DeptManager)</summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<DepartmentDto>>>> GetDepartments(
            [FromQuery] bool? trangThai,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.KhoaPhongs.Include(k => k.DeptManager).AsQueryable();

                if (trangThai.HasValue)
                    query = query.Where(k => k.TrangThai == trangThai);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(k => k.TenKhoa.Contains(search) || k.MaKhoa.Contains(search));

                var total = await query.CountAsync();
                var items = await query
                    .OrderBy(k => k.TenKhoa)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(k => new DepartmentDto
                    {
                        Id = k.Id,
                        MaKhoa = k.MaKhoa,
                        TenKhoa = k.TenKhoa,
                        MoTa = k.MoTa,
                        TrangThai = k.TrangThai,
                        DeptManagerId = k.DeptManagerId,
                        TenQuanLy = k.DeptManager != null ? k.DeptManager.HoTen : null,
                        NgayTao = k.NgayTao,
                        NgayCapNhat = k.NgayCapNhat
                    })
                    .ToListAsync();

                return Ok(new BaseResponseDto<List<DepartmentDto>>
                {
                    Success = true,
                    Message = $"Lấy danh sách khoa thành công ({total} khoa)",
                    Data = items,
                    Pagination = new PaginationDto
                    {
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = total,
                        TotalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments");
                return StatusCode(500, BaseResponseDto<List<DepartmentDto>>.FailureResult("Lỗi server"));
            }
        }

        /// <summary>GET /api/Department/{id}</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<DepartmentDto>>> GetDepartment(int id)
        {
            var khoa = await _context.KhoaPhongs.Include(k => k.DeptManager).FirstOrDefaultAsync(k => k.Id == id);
            if (khoa == null)
                return NotFound(BaseResponseDto<DepartmentDto>.FailureResult("Không tìm thấy khoa"));

            return Ok(new BaseResponseDto<DepartmentDto>
            {
                Success = true,
                Data = new DepartmentDto
                {
                    Id = khoa.Id, MaKhoa = khoa.MaKhoa, TenKhoa = khoa.TenKhoa,
                    MoTa = khoa.MoTa, TrangThai = khoa.TrangThai,
                    DeptManagerId = khoa.DeptManagerId,
                    TenQuanLy = khoa.DeptManager?.HoTen,
                    NgayTao = khoa.NgayTao, NgayCapNhat = khoa.NgayCapNhat
                }
            });
        }

        /// <summary>
        /// GET /api/Department/my-dashboard — Dashboard dành cho DeptManager (chỉ dữ liệu khoa của mình)
        /// </summary>
        [HttpGet("my-dashboard")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<DepartmentDashboardDto>>> GetMyDashboard()
        {
            try
            {
                // Lấy thông tin khoa từ JWT claims
                var khoaId = DepartmentAuthHelper.GetDeptManagerKhoaId(User);
                var khoaName = DepartmentAuthHelper.GetKhoaPhong(User);

                // Admin có thể xem, nhưng cần id khoa cụ thể
                if (khoaId == null && !DepartmentAuthHelper.IsAdmin(User))
                    return BadRequest(BaseResponseDto<DepartmentDashboardDto>.FailureResult("Tài khoản chưa được gán quản lý khoa nào"));

                // Lấy thông tin khoa
                KhoaPhong? khoa = null;
                if (khoaId.HasValue)
                {
                    khoa = await _context.KhoaPhongs.FindAsync(khoaId.Value);
                }
                else if (!string.IsNullOrEmpty(khoaName))
                {
                    khoa = await _context.KhoaPhongs.FirstOrDefaultAsync(k => k.TenKhoa == khoaName);
                }

                if (khoa == null)
                    return BadRequest(BaseResponseDto<DepartmentDashboardDto>.FailureResult("Không tìm thấy thông tin khoa"));

                var tenKhoa = khoa.TenKhoa;

                // 1. Đếm câu hỏi của khoa (theo KhoaPhong string, không bị xóa)
                var tongSoCauHoi = await _context.Cauhois
                    .CountAsync(c => c.KhoaPhong == tenKhoa && c.DaXoa != true);

                // 2. Lấy danh sách kỳ thi của khoa
                var kyThiList = await _context.KyThis
                    .Where(k => k.KhoaPhongId == khoa.Id)
                    .OrderByDescending(k => k.NgayTao)
                    .ToListAsync();

                var tongSoDeThi = kyThiList.Count;

                // 3. Đếm số đề thi của từng kỳ thi và thí sinh
                var kyThiIds = kyThiList.Select(k => k.Id).ToList();

                // Lấy tất cả đề thi thuộc các kỳ thi của khoa
                var dethiList = await _context.Dethis
                    .Where(d => d.KyThiId != null && kyThiIds.Contains(d.KyThiId!.Value))
                    .ToListAsync();

                var dethiIds = dethiList.Select(d => d.Id).ToList();

                // 4. Đếm thí sinh duy nhất đã thi trong các đề của khoa
                var tongSoThiSinh = await _context.Baithis
                    .Where(b => b.IdDeThi != null && dethiIds.Contains(b.IdDeThi!.Value)
                                && b.TrangThai == "Completed")
                    .Select(b => b.IdTaiKhoan)
                    .Distinct()
                    .CountAsync();

                // 5. Tính điểm trung bình
                var diemTrungBinh = 0.0;
                var scores = await _context.Baithis
                    .Where(b => b.IdDeThi != null && dethiIds.Contains(b.IdDeThi!.Value)
                                && b.TrangThai == "Completed"
                                && b.TongDiem != null)
                    .Select(b => b.TongDiem!.Value)
                    .ToListAsync();
                if (scores.Any())
                    diemTrungBinh = scores.Average();

                // 6. Nhóm đề thi theo kỳ thi để lấy số liệu
                var dethiByKyThi = dethiList
                    .Where(d => d.KyThiId.HasValue)
                    .GroupBy(d => d.KyThiId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Đếm thí sinh cho từng kỳ thi
                var baithiByDeThi = await _context.Baithis
                    .Where(b => b.IdDeThi != null && dethiIds.Contains(b.IdDeThi!.Value)
                                && b.TrangThai == "Completed")
                    .GroupBy(b => b.IdDeThi)
                    .Select(g => new { DeThiId = g.Key, SoThiSinh = g.Select(b => b.IdTaiKhoan).Distinct().Count() })
                    .ToListAsync();

                // 7. Build kỳ thi gần đây (5 kỳ mới nhất)
                static string ComputeStatus(KyThi k)
                {
                    if (k.TrangThai == "DaKetThuc" || k.TrangThai == "TamDung") return k.TrangThai;
                    var nowUtc = DateTime.UtcNow;
                    var endUtc = k.ThoiGianKetThuc.HasValue
                        ? DateTime.SpecifyKind(k.ThoiGianKetThuc.Value, DateTimeKind.Utc)
                        : (DateTime?)null;
                    var startUtc = k.ThoiGianBatDau.HasValue
                        ? DateTime.SpecifyKind(k.ThoiGianBatDau.Value, DateTimeKind.Utc)
                        : (DateTime?)null;
                    if (endUtc.HasValue && nowUtc > endUtc.Value) return "DaKetThuc";
                    if (k.TrangThai == "DangDienRa") return "DangDienRa";
                    if (startUtc.HasValue && nowUtc >= startUtc.Value) return "DangDienRa";
                    return "DangChuanBi";
                }

                var kyThiGanDay = kyThiList.Take(5).Select(kt =>
                {
                    var linkedDethis = dethiByKyThi.TryGetValue(kt.Id, out var ds) ? ds : new();
                    var soThiSinhKyThi = linkedDethis
                        .SelectMany(d => baithiByDeThi.Where(b => b.DeThiId == d.Id).Select(b => b.SoThiSinh))
                        .Sum();
                    return new KyThiSummaryDto
                    {
                        Id = kt.Id,
                        TenKyThi = kt.TenKyThi ?? "—",
                        ThoiGianBatDau = kt.ThoiGianBatDau,
                        ThoiGianKetThuc = kt.ThoiGianKetThuc,
                        TrangThai = ComputeStatus(kt),
                        SoDeThi = linkedDethis.Count,
                        SoThiSinh = soThiSinhKyThi,
                    };
                }).ToList();

                var result = new DepartmentDashboardDto
                {
                    IdKhoa = khoa.Id,
                    TenKhoa = tenKhoa,
                    TongSoCauHoi = tongSoCauHoi,
                    TongSoDeThi = tongSoDeThi,
                    TongSoThiSinh = tongSoThiSinh,
                    DiemTrungBinh = Math.Round(diemTrungBinh, 1),
                    KyThiGanDay = kyThiGanDay,
                };

                return Ok(new BaseResponseDto<DepartmentDashboardDto>
                {
                    Success = true,
                    Message = "Lấy dashboard thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department dashboard");
                return StatusCode(500, BaseResponseDto<DepartmentDashboardDto>.FailureResult("Lỗi server khi lấy dashboard"));
            }
        }

        /// <summary>POST /api/Department - Tạo khoa mới (Admin only)</summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            try
            {
                var existing = await _context.KhoaPhongs.AnyAsync(k => k.MaKhoa == dto.MaKhoa);
                if (existing)
                    return BadRequest(BaseResponseDto<DepartmentDto>.FailureResult($"Mã khoa '{dto.MaKhoa}' đã tồn tại"));

                var khoa = new KhoaPhong
                {
                    MaKhoa = dto.MaKhoa,
                    TenKhoa = dto.TenKhoa,
                    MoTa = dto.MoTa,
                    TrangThai = dto.TrangThai,
                    NgayTao = DateTime.Now
                };

                _context.KhoaPhongs.Add(khoa);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDepartment), new { id = khoa.Id }, new BaseResponseDto<DepartmentDto>
                {
                    Success = true,
                    Message = "Tạo khoa thành công",
                    Data = new DepartmentDto { Id = khoa.Id, MaKhoa = khoa.MaKhoa, TenKhoa = khoa.TenKhoa, NgayTao = khoa.NgayTao }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return StatusCode(500, BaseResponseDto<DepartmentDto>.FailureResult("Lỗi server khi tạo khoa"));
            }
        }

        /// <summary>PUT /api/Department/{id} - Cập nhật khoa (Admin only)</summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<DepartmentDto>>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            var khoa = await _context.KhoaPhongs.FindAsync(id);
            if (khoa == null)
                return NotFound(BaseResponseDto<DepartmentDto>.FailureResult("Không tìm thấy khoa"));

            khoa.TenKhoa = dto.TenKhoa;
            khoa.MoTa = dto.MoTa;
            khoa.TrangThai = dto.TrangThai;
            khoa.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new BaseResponseDto<DepartmentDto> { Success = true, Message = "Cập nhật khoa thành công" });
        }

        /// <summary>DELETE /api/Department/{id} - Xóa khoa (Admin only)</summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseDto>> DeleteDepartment(int id)
        {
            var khoa = await _context.KhoaPhongs.FindAsync(id);
            if (khoa == null)
                return NotFound(BaseResponseDto.FailureResult("Không tìm thấy khoa"));

            // Soft delete - đánh dấu inactive thay vì xóa
            khoa.TrangThai = false;
            khoa.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new BaseResponseDto { Success = true, Message = "Đã vô hiệu hóa khoa" });
        }

        /// <summary>POST /api/Department/{id}/assign-manager - Gán quản lý khoa (Admin only)</summary>
        [HttpPost("{id}/assign-manager")]
        public async Task<ActionResult<BaseResponseDto>> AssignManager(int id, [FromBody] AssignManagerDto dto)
        {
            try
            {
                var khoa = await _context.KhoaPhongs.FindAsync(id);
                if (khoa == null)
                    return NotFound(BaseResponseDto.FailureResult("Không tìm thấy khoa"));

                // DeptManagerId = 0 → xóa quản lý
                if (dto.DeptManagerId == 0)
                {
                    // Nếu có quản lý cũ, xóa mapping trên tài khoản đó
                    if (khoa.DeptManagerId.HasValue)
                    {
                        var oldManager = await _context.Taikhoans.FindAsync(khoa.DeptManagerId.Value);
                        if (oldManager != null)
                        {
                            oldManager.IdKhoaQuanLy = null;
                            oldManager.KhoaPhong = null;
                            oldManager.NgayCapNhat = DateTime.Now;
                        }
                    }
                    khoa.DeptManagerId = null;
                    khoa.NgayCapNhat = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok(new BaseResponseDto { Success = true, Message = $"Đã xóa quản lý khỏi {khoa.TenKhoa}" });
                }

                var manager = await _context.Taikhoans.FindAsync(dto.DeptManagerId);
                if (manager == null)
                    return NotFound(BaseResponseDto.FailureResult("Không tìm thấy tài khoản"));

                if (manager.IdVaiTro != 5)
                    return BadRequest(BaseResponseDto.FailureResult("Tài khoản phải có role Quản lý Khoa (ID=5)"));

                // Nếu khoa đã có quản lý khác, xóa mapping cũ
                if (khoa.DeptManagerId.HasValue && khoa.DeptManagerId != dto.DeptManagerId)
                {
                    var oldManager = await _context.Taikhoans.FindAsync(khoa.DeptManagerId.Value);
                    if (oldManager != null)
                    {
                        oldManager.IdKhoaQuanLy = null;
                        oldManager.KhoaPhong = null;
                        oldManager.NgayCapNhat = DateTime.Now;
                    }
                }

                // Assign manager to department
                khoa.DeptManagerId = dto.DeptManagerId;
                khoa.NgayCapNhat = DateTime.Now;

                // Also update IdKhoaQuanLy and KhoaPhong string on the user
                // KhoaPhong string is used in JWT claim "khoa_phong" for filtering
                manager.IdKhoaQuanLy = id;
                manager.KhoaPhong = khoa.TenKhoa;  // sync string for JWT
                manager.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new BaseResponseDto { Success = true, Message = $"Đã gán {manager.HoTen} làm quản lý {khoa.TenKhoa}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning manager");
                return StatusCode(500, BaseResponseDto.FailureResult("Lỗi server"));
            }
        }

        /// <summary>POST /api/Department/exam/{deThiId}/toggle-visibility - Bật/tắt công bố điểm theo đề thi</summary>
        [HttpPost("exam/{deThiId}/toggle-visibility")]
        public async Task<ActionResult<BaseResponseDto>> ToggleExamVisibility(int deThiId, [FromBody] ExamVisibilityDto dto)
        {
            try
            {
                var deThi = await _context.Dethis.FindAsync(deThiId);
                if (deThi == null)
                    return NotFound(BaseResponseDto.FailureResult("Không tìm thấy đề thi"));

                if (DepartmentAuthHelper.IsDeptManager(User))
                {
                    var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                    if (deThi.KhoaPhong != myKhoa)
                        return Forbid();
                }
                if (dto.CongBoKetQua)
                {
                    var hasUngraded = await _context.Chitietlambais
                        .AnyAsync(c => c.IdBaiThiNavigation != null 
                                    && c.IdBaiThiNavigation.IdDeThi == deThiId 
                                    && c.IdBaiThiNavigation.TrangThai == "Completed"
                                    && c.IdCauHoiNavigation != null
                                    && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null
                                    && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan")
                                    && c.DiemDatDuoc == null);
                    if (hasUngraded)
                    {
                        return BadRequest(BaseResponseDto.FailureResult("Không thể công bố điểm vì còn câu hỏi tự luận chưa được chấm."));
                    }
                }

                deThi.CongBoKetQua = dto.CongBoKetQua;
                deThi.ThoiGianCongBo = dto.CongBoKetQua ? DateTime.Now : null;

                await _context.SaveChangesAsync();

                var msg = dto.CongBoKetQua ? "Đã bật công bố kết quả" : "Đã tắt công bố kết quả";
                return Ok(new BaseResponseDto { Success = true, Message = msg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling exam visibility");
                return StatusCode(500, BaseResponseDto.FailureResult("Lỗi server"));
            }
        }

        /// <summary>
        /// GET /api/Department/import-template
        /// Download template Excel để import danh sách Khoa/Phòng ban (Admin only)
        /// </summary>
        [HttpGet("import-template")]
        public IActionResult DownloadImportTemplate()
        {
            using var workbook = new XLWorkbook();

            var wsGuide = workbook.Worksheets.Add("HUONG_DAN");
            wsGuide.Cell("A1").Value = "TEMPLATE IMPORT KHOA/PHONG BAN";
            wsGuide.Cell("A1").Style.Font.Bold = true;
            wsGuide.Cell("A1").Style.Font.FontSize = 14;
            wsGuide.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;

            var guide = new (string col, string desc)[]
            {
                ("Cột", "Mô tả"),
                ("A - Mã Khoa (*)", "Bắt buộc. Viết hoa, dùng _ thay space. VD: KHOA_NOI"),
                ("B - Tên Khoa (*)", "Bắt buộc. Tên đầy đủ. VD: Khoa Nội"),
                ("C - Trạng thái", "HoatDong hoặc TamDung (mặc định: HoatDong)"),
                ("D - Mô tả", "Không bắt buộc"),
            };

            for (int i = 0; i < guide.Length; i++)
            {
                wsGuide.Cell(i + 3, 1).Value = guide[i].col;
                wsGuide.Cell(i + 3, 2).Value = guide[i].desc;
                if (i == 0) { wsGuide.Cell(i + 3, 1).Style.Font.Bold = true; wsGuide.Cell(i + 3, 2).Style.Font.Bold = true; }
            }
            wsGuide.Column(1).Width = 30;
            wsGuide.Column(2).Width = 60;

            var ws = workbook.Worksheets.Add("IMPORT_KHOA_PHONG");
            var headers = new[] { "Mã Khoa (*)", "Tên Khoa (*)", "Trạng thái (HoatDong/TamDung)", "Mô tả" };
            var widths = new[] { 22, 40, 35, 50 };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#833C11");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Column(c + 1).Width = widths[c];
            }
            ws.Row(1).Height = 35;

            var samples = new[]
            {
                ("KHOA_NOI", "Khoa Nội", "HoatDong", "Khoa điều trị bệnh nội khoa"),
                ("KHOA_NGOAI", "Khoa Ngoại", "HoatDong", "Khoa phẫu thuật ngoại khoa"),
                ("KHOA_NHI", "Khoa Nhi", "HoatDong", "Khoa điều trị trẻ em"),
                ("KHOA_SAN", "Khoa Sản", "HoatDong", "Khoa sản phụ khoa"),
                ("KHOA_CAPCU", "Khoa Cấp cứu", "HoatDong", "Khoa cấp cứu và hồi sức"),
            };

            for (int r = 0; r < samples.Length; r++)
            {
                ws.Cell(r + 2, 1).Value = samples[r].Item1;
                ws.Cell(r + 2, 2).Value = samples[r].Item2;
                ws.Cell(r + 2, 3).Value = samples[r].Item3;
                ws.Cell(r + 2, 4).Value = samples[r].Item4;
                for (int c = 1; c <= 4; c++)
                    ws.Cell(r + 2, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "template_import_khoaphong.xlsx");
        }

        /// <summary>
        /// POST /api/Department/import
        /// Import danh sách Khoa/Phòng từ file Excel (Admin only)
        /// </summary>
        private class DepartmentImportRecord
        {
            public int Row { get; set; }
            public string TenKhoa { get; set; } = string.Empty;
            public string MaKhoa { get; set; } = string.Empty;
            public string TrangThai { get; set; } = string.Empty;
            public string MoTa { get; set; } = string.Empty;
        }

        /// <summary>
        /// POST /api/Department/import
        /// Import danh sách Khoa/Phòng từ file Excel/CSV (Admin only)
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> ImportDepartments(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File không hợp lệ hoặc rỗng" });

            var errors = new List<string>();
            var records = new List<DepartmentImportRecord>();
            int created = 0, skipped = 0;

            try
            {
                using var stream = file.OpenReadStream();

                if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(stream);
                    var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        MissingFieldFound = null,
                        HeaderValidated = null,
                        BadDataFound = null,
                    };
                    using var csv = new CsvHelper.CsvReader(reader, config);
                    csv.Read();
                    csv.ReadHeader();
                    var headerRecord = csv.HeaderRecord;

                    int colTenKhoa = -1, colMaKhoa = -1, colTrangThai = -1, colMoTa = -1;
                    if (headerRecord != null)
                    {
                        for (int col = 0; col < headerRecord.Length; col++)
                        {
                            var headerText = headerRecord[col]?.Trim() ?? string.Empty;
                            if (string.IsNullOrEmpty(headerText)) continue;

                            if (headerText.Contains("Mã Khoa", StringComparison.OrdinalIgnoreCase) || 
                                headerText.Contains("Ma Khoa", StringComparison.OrdinalIgnoreCase))
                            {
                                colMaKhoa = col;
                            }
                            else if (headerText.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase) || 
                                     headerText.Contains("Trang thai", StringComparison.OrdinalIgnoreCase))
                            {
                                colTrangThai = col;
                            }
                            else if (headerText.Contains("Mô tả", StringComparison.OrdinalIgnoreCase) || 
                                     headerText.Contains("Mo ta", StringComparison.OrdinalIgnoreCase))
                            {
                                colMoTa = col;
                            }
                            else if (headerText.Contains("khoa", StringComparison.OrdinalIgnoreCase) || 
                                     headerText.Contains("phòng", StringComparison.OrdinalIgnoreCase) || 
                                     headerText.Contains("phong", StringComparison.OrdinalIgnoreCase) ||
                                     headerText.Contains("department", StringComparison.OrdinalIgnoreCase))
                            {
                                if (colTenKhoa == -1) colTenKhoa = col;
                            }
                        }
                    }

                    if (colTenKhoa == -1 && headerRecord != null)
                    {
                        if (headerRecord.Length == 1)
                        {
                            colTenKhoa = 0;
                        }
                        else if (headerRecord.Length == 2)
                        {
                            var h1 = headerRecord[0]?.Trim() ?? string.Empty;
                            var h2 = headerRecord[1]?.Trim() ?? string.Empty;
                            if (h1.Equals("STT", StringComparison.OrdinalIgnoreCase) || double.TryParse(h1, out _))
                            {
                                colTenKhoa = 1;
                            }
                            else if (h2.Equals("STT", StringComparison.OrdinalIgnoreCase) || double.TryParse(h2, out _))
                            {
                                colTenKhoa = 0;
                            }
                            else
                            {
                                colTenKhoa = 1;
                                colMaKhoa = 0;
                            }
                        }
                    }

                    if (colTenKhoa == -1)
                    {
                        return BadRequest(new { success = false, message = "Không tìm thấy cột chứa tên Khoa/Phòng trong file CSV (cột cần có tiêu đề chứa chữ 'Khoa' hoặc 'Phòng')" });
                    }

                    int csvRowNumber = 1;
                    while (csv.Read())
                    {
                        csvRowNumber++;
                        string ten = colTenKhoa >= 0 && colTenKhoa < csv.Parser.Count ? csv.GetField(colTenKhoa)?.Trim() ?? string.Empty : string.Empty;
                        if (string.IsNullOrWhiteSpace(ten)) continue;

                        string ma = colMaKhoa >= 0 && colMaKhoa < csv.Parser.Count ? csv.GetField(colMaKhoa)?.Trim() ?? string.Empty : string.Empty;
                        string tt = colTrangThai >= 0 && colTrangThai < csv.Parser.Count ? csv.GetField(colTrangThai)?.Trim() ?? string.Empty : string.Empty;
                        string mt = colMoTa >= 0 && colMoTa < csv.Parser.Count ? csv.GetField(colMoTa)?.Trim() ?? string.Empty : string.Empty;

                        records.Add(new DepartmentImportRecord
                        {
                            Row = csvRowNumber,
                            TenKhoa = ten,
                            MaKhoa = ma,
                            TrangThai = tt,
                            MoTa = mt
                        });
                    }
                }
                else
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using var reader = ExcelReaderFactory.CreateReader(stream);
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    var table = result.Tables.Cast<DataTable>()
                        .FirstOrDefault(t => t.TableName.Contains("IMPORT", StringComparison.OrdinalIgnoreCase) || 
                                             t.TableName.Contains("KHOA", StringComparison.OrdinalIgnoreCase))
                        ?? result.Tables.Cast<DataTable>().FirstOrDefault();

                    if (table == null || table.Rows.Count == 0)
                        return BadRequest(new { success = false, message = "File Excel không có dữ liệu" });

                    int colTenKhoa = -1, colMaKhoa = -1, colTrangThai = -1, colMoTa = -1;
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        var headerText = table.Columns[col].ColumnName.Trim();
                        if (string.IsNullOrEmpty(headerText)) continue;

                        if (headerText.Contains("Mã Khoa", StringComparison.OrdinalIgnoreCase) || 
                            headerText.Contains("Ma Khoa", StringComparison.OrdinalIgnoreCase))
                        {
                            colMaKhoa = col;
                        }
                        else if (headerText.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase) || 
                                 headerText.Contains("Trang thai", StringComparison.OrdinalIgnoreCase))
                        {
                            colTrangThai = col;
                        }
                        else if (headerText.Contains("Mô tả", StringComparison.OrdinalIgnoreCase) || 
                                 headerText.Contains("Mo ta", StringComparison.OrdinalIgnoreCase))
                        {
                            colMoTa = col;
                        }
                        else if (headerText.Contains("khoa", StringComparison.OrdinalIgnoreCase) || 
                                 headerText.Contains("phòng", StringComparison.OrdinalIgnoreCase) || 
                                 headerText.Contains("phong", StringComparison.OrdinalIgnoreCase) ||
                                 headerText.Contains("department", StringComparison.OrdinalIgnoreCase))
                        {
                            if (colTenKhoa == -1)
                            {
                                colTenKhoa = col;
                            }
                            else
                            {
                                var prevText = table.Columns[colTenKhoa].ColumnName.Trim();
                                bool prevHasBoth = prevText.Contains("khoa", StringComparison.OrdinalIgnoreCase) && 
                                                   (prevText.Contains("phòng", StringComparison.OrdinalIgnoreCase) || prevText.Contains("phong", StringComparison.OrdinalIgnoreCase));
                                bool currHasBoth = headerText.Contains("khoa", StringComparison.OrdinalIgnoreCase) && 
                                                   (headerText.Contains("phòng", StringComparison.OrdinalIgnoreCase) || headerText.Contains("phong", StringComparison.OrdinalIgnoreCase));
                                if (currHasBoth && !prevHasBoth)
                                {
                                    colTenKhoa = col;
                                }
                            }
                        }
                    }

                    if (colTenKhoa == -1)
                    {
                        if (table.Columns.Count == 1)
                        {
                            colTenKhoa = 0;
                        }
                        else if (table.Columns.Count == 2)
                        {
                            var h1 = table.Columns[0].ColumnName.Trim();
                            var h2 = table.Columns[1].ColumnName.Trim();
                            if (h1.Equals("STT", StringComparison.OrdinalIgnoreCase) || double.TryParse(h1, out _))
                            {
                                colTenKhoa = 1;
                            }
                            else if (h2.Equals("STT", StringComparison.OrdinalIgnoreCase) || double.TryParse(h2, out _))
                            {
                                colTenKhoa = 0;
                            }
                            else
                            {
                                colTenKhoa = 1;
                                colMaKhoa = 0;
                            }
                        }
                    }

                    if (colTenKhoa == -1)
                    {
                        return BadRequest(new { success = false, message = "Không tìm thấy cột chứa tên Khoa/Phòng trong file Excel (cột cần có tiêu đề chứa chữ 'Khoa' hoặc 'Phòng')" });
                    }

                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var rowData = table.Rows[i];
                        string ten = rowData[colTenKhoa]?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(ten)) continue;

                        string ma = colMaKhoa != -1 ? rowData[colMaKhoa]?.ToString()?.Trim() ?? string.Empty : string.Empty;
                        string tt = colTrangThai != -1 ? rowData[colTrangThai]?.ToString()?.Trim() ?? string.Empty : string.Empty;
                        string mt = colMoTa != -1 ? rowData[colMoTa]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                        records.Add(new DepartmentImportRecord
                        {
                            Row = i + 2,
                            TenKhoa = ten,
                            MaKhoa = ma,
                            TrangThai = tt,
                            MoTa = mt
                        });
                    }
                }

                // Load existing departments into memory to avoid duplicate checking
                var existingDepartments = await _context.KhoaPhongs.ToListAsync();
                var existingMaKhoas = new HashSet<string>(existingDepartments.Select(k => k.MaKhoa), StringComparer.OrdinalIgnoreCase);
                var existingTenKhoas = new HashSet<string>(existingDepartments.Select(k => k.TenKhoa), StringComparer.OrdinalIgnoreCase);

                foreach (var rec in records)
                {
                    string tenKhoa = rec.TenKhoa;
                    string maKhoa = rec.MaKhoa.ToUpper();
                    string moTa = rec.MoTa;
                    string trangThaiStr = rec.TrangThai;

                    bool trangThai = string.IsNullOrWhiteSpace(trangThaiStr) || 
                                     trangThaiStr.Equals("HoatDong", StringComparison.OrdinalIgnoreCase) ||
                                     trangThaiStr.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase) ||
                                     trangThaiStr.Equals("Hoat Dong", StringComparison.OrdinalIgnoreCase) ||
                                     trangThaiStr.Equals("1") ||
                                     trangThaiStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                    // Check duplicate by TenKhoa
                    if (existingTenKhoas.Contains(tenKhoa))
                    {
                        skipped++;
                        errors.Add($"Dòng {rec.Row}: Khoa/phòng '{tenKhoa}' đã tồn tại — bỏ qua");
                        continue;
                    }

                    bool isAutoGenerateMaKhoa = string.IsNullOrEmpty(maKhoa);
                    if (isAutoGenerateMaKhoa)
                    {
                        maKhoa = GenerateMaKhoa(tenKhoa);
                        if (string.IsNullOrEmpty(maKhoa))
                        {
                            skipped++;
                            errors.Add($"Dòng {rec.Row}: Tên Khoa không hợp lệ để tạo Mã Khoa");
                            continue;
                        }

                        // Ensure MaKhoa is unique
                        int suffix = 1;
                        string baseMaKhoa = maKhoa;
                        while (existingMaKhoas.Contains(maKhoa))
                        {
                            string suffixStr = $"_{suffix}";
                            if (baseMaKhoa.Length + suffixStr.Length > 50)
                            {
                                maKhoa = baseMaKhoa.Substring(0, 50 - suffixStr.Length) + suffixStr;
                            }
                            else
                            {
                                maKhoa = baseMaKhoa + suffixStr;
                            }
                            suffix++;
                        }
                    }
                    else
                    {
                        if (existingMaKhoas.Contains(maKhoa))
                        {
                            skipped++;
                            errors.Add($"Dòng {rec.Row}: Mã khoa '{maKhoa}' đã tồn tại — bỏ qua");
                            continue;
                        }
                    }

                    _context.KhoaPhongs.Add(new KhoaPhong
                    {
                        MaKhoa = maKhoa,
                        TenKhoa = tenKhoa,
                        TrangThai = trangThai,
                        MoTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa,
                        NgayTao = DateTime.Now,
                    });

                    existingMaKhoas.Add(maKhoa);
                    existingTenKhoas.Add(tenKhoa);
                    created++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Import thành công {created} khoa/phòng. Bỏ qua: {skipped}.",
                    created,
                    skipped,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing departments from file");
                return StatusCode(500, new { success = false, message = "Lỗi xử lý file: " + ex.Message });
            }
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var result = stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
            var finalBuilder = new System.Text.StringBuilder();
            foreach (var c in result)
            {
                if (c == 'đ') finalBuilder.Append('d');
                else if (c == 'Đ') finalBuilder.Append('D');
                else finalBuilder.Append(c);
            }
            return finalBuilder.ToString();
        }

        private static string GenerateMaKhoa(string tenKhoa)
        {
            if (string.IsNullOrWhiteSpace(tenKhoa))
                return string.Empty;

            string noDiacritics = RemoveDiacritics(tenKhoa);

            var sb = new System.Text.StringBuilder();
            bool lastWasUnderscore = false;

            foreach (char c in noDiacritics)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                    lastWasUnderscore = false;
                }
                else if (c == ' ' || c == '_' || c == '-' || c == '/' || c == '\\')
                {
                    if (!lastWasUnderscore && sb.Length > 0)
                    {
                        sb.Append('_');
                        lastWasUnderscore = true;
                    }
                }
            }

            string result = sb.ToString();
            if (result.EndsWith("_"))
                result = result.Substring(0, result.Length - 1);

            if (result.Length > 50)
            {
                result = result.Substring(0, 50);
                if (result.EndsWith("_"))
                    result = result.Substring(0, result.Length - 1);
            }

            return result;
        }
    }
}
