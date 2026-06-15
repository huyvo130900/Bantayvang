using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Department;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    Data = items
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
        [HttpPost("import")]
        public async Task<IActionResult> ImportDepartments(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File không hợp lệ" });

            var errors = new List<string>();
            int created = 0, skipped = 0;

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var ws = workbook.Worksheets
                    .FirstOrDefault(w => w.Name.Contains("IMPORT") || w.Name.Contains("KHOA"))
                    ?? workbook.Worksheets.First();

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                if (lastRow < 2)
                    return BadRequest(new { success = false, message = "File Excel không có dữ liệu" });

                // Detect headers in row 1
                var headerA = ws.Cell(1, 1).GetString().Trim();
                var headerB = ws.Cell(1, 2).GetString().Trim();

                bool isNewFormat = false;
                if (headerA.Equals("STT", StringComparison.OrdinalIgnoreCase) &&
                    (headerB.Contains("Khoa", StringComparison.OrdinalIgnoreCase) ||
                     headerB.Contains("phòng", StringComparison.OrdinalIgnoreCase) ||
                     headerB.Contains("phong", StringComparison.OrdinalIgnoreCase)))
                {
                    isNewFormat = true;
                }

                // Load existing departments into memory to avoid N+1 DB queries and duplicate checking
                var existingDepartments = await _context.KhoaPhongs.ToListAsync();
                var existingMaKhoas = new HashSet<string>(existingDepartments.Select(k => k.MaKhoa), StringComparer.OrdinalIgnoreCase);
                var existingTenKhoas = new HashSet<string>(existingDepartments.Select(k => k.TenKhoa), StringComparer.OrdinalIgnoreCase);

                for (int row = 2; row <= lastRow; row++)
                {
                    string maKhoa = string.Empty;
                    string tenKhoa = string.Empty;
                    bool trangThai = true;
                    string? moTa = null;

                    if (isNewFormat)
                    {
                        tenKhoa = ws.Cell(row, 2).GetString().Trim();
                    }
                    else
                    {
                        maKhoa = ws.Cell(row, 1).GetString().Trim().ToUpper();
                        tenKhoa = ws.Cell(row, 2).GetString().Trim();
                        var trangThaiStr = ws.Cell(row, 3).GetString().Trim();
                        moTa = ws.Cell(row, 4).GetString().Trim();
                        
                        // Default status is HoatDong if blank
                        trangThai = string.IsNullOrWhiteSpace(trangThaiStr) || trangThaiStr.Equals("HoatDong", StringComparison.OrdinalIgnoreCase);
                    }

                    if (string.IsNullOrWhiteSpace(tenKhoa))
                        continue;

                    // Check duplicate by TenKhoa
                    if (existingTenKhoas.Contains(tenKhoa))
                    {
                        skipped++;
                        errors.Add($"Dòng {row}: Khoa/phòng '{tenKhoa}' đã tồn tại — bỏ qua");
                        continue;
                    }

                    if (isNewFormat)
                    {
                        maKhoa = GenerateMaKhoa(tenKhoa);
                        if (string.IsNullOrEmpty(maKhoa))
                        {
                            errors.Add($"Dòng {row}: Tên Khoa không hợp lệ để tạo Mã Khoa");
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
                        if (string.IsNullOrWhiteSpace(maKhoa))
                        {
                            errors.Add($"Dòng {row}: Mã Khoa không được để trống");
                            continue;
                        }

                        if (existingMaKhoas.Contains(maKhoa))
                        {
                            skipped++;
                            errors.Add($"Dòng {row}: Mã khoa '{maKhoa}' đã tồn tại — bỏ qua");
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
                _logger.LogError(ex, "Error importing departments from Excel");
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
