using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace BanTayVang.API.Services.Impl
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ITaikhoanRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            ITaikhoanRepository userRepository,
            IPasswordService passwordService,
            BanTayVangDbContext context,
            ILogger<UserManagementService> logger)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<List<UserDto>>> GetAllUsersAsync(UserFilterDto filter)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var query = users.AsQueryable();

                if (filter.IdVaiTro.HasValue)
                    query = query.Where(u => u.IdVaiTro == filter.IdVaiTro);

                if (filter.TrangThai.HasValue)
                    query = query.Where(u => u.TrangThai == filter.TrangThai);

                if (!string.IsNullOrEmpty(filter.KhoaPhong))
                    query = query.Where(u => u.KhoaPhong == filter.KhoaPhong);

                if (!string.IsNullOrEmpty(filter.SearchKeyword))
                {
                    var keyword = filter.SearchKeyword.ToLower();
                    query = query.Where(u => 
                        (u.TenDangNhap ?? "").ToLower().Contains(keyword) ||
                        (u.HoTen ?? "").ToLower().Contains(keyword));
                }

                var pagedUsers = query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(u => MapToDto(u))
                    .ToList();

                return new BaseResponseDto<List<UserDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách người dùng thành công",
                    Data = pagedUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return new BaseResponseDto<List<UserDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách người dùng",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<UserDto>> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Không tìm thấy người dùng" };

                return new BaseResponseDto<UserDto>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = MapToDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user");
                return new BaseResponseDto<UserDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<UserDto>> CreateUserAsync(CreateUserDto createDto)
        {
            try
            {
                var existing = await _userRepository.GetByUsernameOrEmailAsync(createDto.TenDangNhap);
                if (existing != null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại" };

                if (!string.IsNullOrWhiteSpace(createDto.MaNhanVien))
                {
                    var existingByEmpCode = await _context.Taikhoans.FirstOrDefaultAsync(u => u.MaNhanVien == createDto.MaNhanVien);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại" };
                }

                // Validate department manager assignment
                if (createDto.IdVaiTro == 5 && createDto.IdKhoaQuanLy.HasValue)
                {
                    var khoa = await _context.KhoaPhongs.FindAsync(createDto.IdKhoaQuanLy.Value);
                    if (khoa != null && khoa.DeptManagerId.HasValue)
                    {
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Khoa này đã có người quản lý. Không thể tạo thêm." };
                    }
                }

                var user = new Taikhoan
                {
                    TenDangNhap = createDto.TenDangNhap,
                    MatKhau = _passwordService.HashPassword(createDto.MatKhau),
                    HoTen = createDto.HoTen,
                    MaNhanVien = createDto.MaNhanVien,
                    ChucDanh = createDto.ChucDanh,
                    KhoaPhong = createDto.KhoaPhong,
                    IdVaiTro = createDto.IdVaiTro,
                    TrangThai = createDto.TrangThai,
                    NgayTao = DateTime.Now,
                    IdKhoaQuanLy = createDto.IdVaiTro == 5 ? createDto.IdKhoaQuanLy : null,
                    Email = createDto.Email,
                    SoDienThoai = createDto.SoDienThoai
                };

                var saved = await _userRepository.AddAsync(user);

                // Auto-assign DeptManager to KhoaPhong and sync KhoaPhong string from Khoa name
                if (createDto.IdVaiTro == 5 && createDto.IdKhoaQuanLy.HasValue)
                {
                    var khoa = await _context.KhoaPhongs.FindAsync(createDto.IdKhoaQuanLy.Value);
                    if (khoa != null)
                    {
                        // Update DeptManagerId on the department
                        khoa.DeptManagerId = saved.Id;
                        khoa.NgayCapNhat = DateTime.Now;
                        // Sync KhoaPhong string on user for JWT claim
                        saved.KhoaPhong = khoa.TenKhoa;
                        await _context.SaveChangesAsync();
                    }
                }

                return new BaseResponseDto<UserDto>
                {
                    Success = true,
                    Message = "Tạo người dùng thành công",
                    Data = MapToDto(saved)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new BaseResponseDto<UserDto> { Success = false, Message = "Lỗi tạo người dùng", Errors = new List<string> { ex.Message } };
            }
        }

        public async Task<BaseResponseDto<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Không tìm thấy người dùng" };

                if (!string.IsNullOrWhiteSpace(updateDto.MaNhanVien))
                {
                    var existingByEmpCode = await _context.Taikhoans.FirstOrDefaultAsync(u => u.MaNhanVien == updateDto.MaNhanVien && u.Id != id);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại" };
                }

                // Validate new department manager assignment before proceeding
                if (updateDto.IdVaiTro == 5 && updateDto.IdKhoaQuanLy.HasValue)
                {
                    var khoa = await _context.KhoaPhongs.FindAsync(updateDto.IdKhoaQuanLy.Value);
                    if (khoa != null && khoa.DeptManagerId.HasValue && khoa.DeptManagerId.Value != user.Id)
                    {
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Khoa này đã có người quản lý. Không thể gán thêm." };
                    }
                }

                // Clear old department manager mapping if they were previously managing a department
                if (user.IdVaiTro == 5 && user.IdKhoaQuanLy.HasValue)
                {
                    var oldKhoa = await _context.KhoaPhongs.FindAsync(user.IdKhoaQuanLy.Value);
                    if (oldKhoa != null && oldKhoa.DeptManagerId == user.Id)
                    {
                        oldKhoa.DeptManagerId = null;
                        oldKhoa.NgayCapNhat = DateTime.Now;
                    }
                }

                user.HoTen = updateDto.HoTen;
                user.MaNhanVien = updateDto.MaNhanVien;
                user.ChucDanh = updateDto.ChucDanh;
                user.KhoaPhong = updateDto.KhoaPhong;
                user.IdVaiTro = updateDto.IdVaiTro;
                user.TrangThai = updateDto.TrangThai;
                user.NgayCapNhat = DateTime.Now;
                user.Email = updateDto.Email;
                user.SoDienThoai = updateDto.SoDienThoai;

                // Handle new department manager assignment
                if (updateDto.IdVaiTro == 5 && updateDto.IdKhoaQuanLy.HasValue)
                {
                    user.IdKhoaQuanLy = updateDto.IdKhoaQuanLy.Value;
                    var khoa = await _context.KhoaPhongs.FindAsync(updateDto.IdKhoaQuanLy.Value);
                    if (khoa != null)
                    {
                        khoa.DeptManagerId = user.Id;
                        khoa.NgayCapNhat = DateTime.Now;
                        // Sync string description for JWT claim
                        user.KhoaPhong = khoa.TenKhoa;
                    }
                }
                else
                {
                    user.IdKhoaQuanLy = null;
                }

                await _userRepository.UpdateAsync(user);

                return new BaseResponseDto<UserDto>
                {
                    Success = true,
                    Message = "Cập nhật thành công",
                    Data = MapToDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new BaseResponseDto<UserDto> { Success = false, Message = "Lỗi cập nhật", Errors = new List<string> { ex.Message } };
            }
        }

        public async Task<BaseResponseDto> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };

                user.TrangThai = false;
                user.NgayCapNhat = DateTime.Now;

                // Clear department manager mapping if they are being deactivated/deleted
                if (user.IdVaiTro == 5 && user.IdKhoaQuanLy.HasValue)
                {
                    var khoa = await _context.KhoaPhongs.FindAsync(user.IdKhoaQuanLy.Value);
                    if (khoa != null && khoa.DeptManagerId == user.Id)
                    {
                        khoa.DeptManagerId = null;
                        khoa.NgayCapNhat = DateTime.Now;
                    }
                    user.IdKhoaQuanLy = null;
                }

                await _userRepository.UpdateAsync(user);

                return new BaseResponseDto { Success = true, Message = "Đã vô hiệu hóa tài khoản" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> ActivateUserAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };

                user.TrangThai = true;
                user.NgayCapNhat = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                return new BaseResponseDto { Success = true, Message = "Đã kích hoạt tài khoản" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> ResetUserPasswordAsync(int id, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                    return new BaseResponseDto { Success = false, Message = "Mật khẩu phải từ 6 ký tự" };

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };

                user.MatKhau = _passwordService.HashPassword(newPassword);
                user.NgayCapNhat = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                return new BaseResponseDto { Success = true, Message = "Đã đặt lại mật khẩu" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeleteUserAsync(int id)
        {
            try
            {
                // Soft delete - just deactivate
                return await DeactivateUserAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        private static UserDto MapToDto(Taikhoan u)
        {
            return new UserDto
            {
                Id = u.Id,
                MaNhanVien = u.MaNhanVien,
                TenDangNhap = u.TenDangNhap,
                HoTen = u.HoTen,
                ChucDanh = u.ChucDanh,
                KhoaPhong = u.KhoaPhong,
                IdVaiTro = u.IdVaiTro,
                TenVaiTro = GetRoleName(u.IdVaiTro),
                IdKhoaQuanLy = u.IdKhoaQuanLy,
                TenKhoaQuanLy = u.KhoaQuanLy?.TenKhoa,
                TrangThai = u.TrangThai,
                NgayTao = u.NgayTao,
                LanDangNhapCuoi = u.LanDangNhapCuoi,
                Email = u.Email,
                SoDienThoai = u.SoDienThoai
            };
        }

        private static string GetRoleName(int? roleId) => roleId switch
        {
            1 => "Admin",
            2 => "Teacher",   // Obsolete
            3 => "Student",
            4 => "Supervisor", // Obsolete
            5 => "DeptManager",
            _ => "Unknown"
        };

        public Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync()
        {
            try
            {
                using var workbook = new XLWorkbook();

                var wsGuide = workbook.Worksheets.Add("HUONG_DAN");
                wsGuide.Cell("A1").Value = "HUỚNG DẪN IMPORT DANH SÁCH TÀI KHOẢN";
                wsGuide.Cell("A1").Style.Font.Bold = true;
                wsGuide.Cell("A1").Style.Font.FontSize = 14;
                wsGuide.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;

                var guide = new (string col, string desc)[]
                {
                    ("Cột", "Mô tả"),
                    ("A - STT", "Không bắt buộc. Số thứ tự."),
                    ("B - Tài khoản (*)", "Bắt buộc. Dùng làm tên đăng nhập và mã nhân viên. VD: NV001"),
                    ("C - Họ tên (*)", "Bắt buộc. Họ và tên đầy đủ của người dùng. VD: Nguyễn Văn A"),
                    ("D - Email", "Không bắt buộc. Địa chỉ email liên hệ. VD: nguyenvana@example.com"),
                    ("E - Số điện thoại", "Không bắt buộc. Số điện thoại liên hệ. VD: 0912345678"),
                    ("F - Khoa/phòng", "Không bắt buộc. Tên khoa phòng công tác. VD: Khoa Nội")
                };

                for (int i = 0; i < guide.Length; i++)
                {
                    wsGuide.Cell(i + 3, 1).Value = guide[i].col;
                    wsGuide.Cell(i + 3, 2).Value = guide[i].desc;
                    if (i == 0)
                    {
                        wsGuide.Cell(i + 3, 1).Style.Font.Bold = true;
                        wsGuide.Cell(i + 3, 2).Style.Font.Bold = true;
                    }
                }
                wsGuide.Column(1).Width = 30;
                wsGuide.Column(2).Width = 80;

                var ws = workbook.Worksheets.Add("IMPORT_TAI_KHOAN");
                var headers = new[] { "STT", "Tài khoản (*)", "Họ tên (*)", "Email", "Số điện thoại", "Khoa/phòng" };
                var widths = new[] { 10, 22, 30, 25, 20, 25 };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cell(1, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Column(c + 1).Width = widths[c];
                }
                ws.Row(1).Height = 30;

                // Add sample data
                var samples = new[]
                {
                    ("1", "NV001", "Nguyễn Văn A", "nguyenvana@example.com", "0912345678", "Khoa Nội"),
                    ("2", "NV002", "Trần Thị B", "tranthib@example.com", "0923456789", "Khoa Ngoại"),
                    ("3", "NV003", "Phạm Văn C", "phamvanc@example.com", "0934567890", "Khoa Nhi")
                };

                for (int r = 0; r < samples.Length; r++)
                {
                    ws.Cell(r + 2, 1).Value = samples[r].Item1;
                    ws.Cell(r + 2, 2).Value = samples[r].Item2;
                    ws.Cell(r + 2, 3).Value = samples[r].Item3;
                    ws.Cell(r + 2, 4).Value = samples[r].Item4;
                    ws.Cell(r + 2, 5).Value = samples[r].Item5;
                    ws.Cell(r + 2, 6).Value = samples[r].Item6;
                    for (int c = 1; c <= 6; c++)
                        ws.Cell(r + 2, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                ws.SheetView.FreezeRows(1);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return Task.FromResult(new BaseResponseDto<byte[]>
                {
                    Success = true,
                    Message = "Tạo file mẫu thành công",
                    Data = stream.ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user import template");
                return Task.FromResult(new BaseResponseDto<byte[]>
                {
                    Success = false,
                    Message = "Lỗi khi tạo file mẫu: " + ex.Message
                });
            }
        }

        public async Task<BaseResponseDto<ExcelImportResultDto>> ImportUsersFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new BaseResponseDto<ExcelImportResultDto>
                {
                    Success = false,
                    Message = "File không hợp lệ hoặc rỗng."
                };
            }

            var resultDto = new ExcelImportResultDto();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var ws = workbook.Worksheets
                    .FirstOrDefault(w => w.Name.Contains("IMPORT") || w.Name.Contains("TAI_KHOAN") || w.Name.Contains("USER"))
                    ?? workbook.Worksheets.First();

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                int colTaiKhoan = -1;
                int colHoTen = -1;
                int colEmail = -1;
                int colSoDienThoai = -1;
                int colKhoaPhong = -1;
                int colMatKhau = -1;
                int colVaiTro = -1;
                int colChucDanh = -1;

                // Scan headers in Row 1
                var firstRow = ws.Row(1);
                int lastCell = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 8;
                for (int col = 1; col <= lastCell; col++)
                {
                    var headerText = firstRow.Cell(col).GetString().Trim().ToLower();
                    if (string.IsNullOrEmpty(headerText)) continue;

                    if (headerText.Contains("tài khoản") || headerText.Contains("taikhoan") || 
                        headerText.Contains("tên đăng nhập") || headerText.Contains("tendangnhap") || 
                        headerText.Contains("mã nhân viên") || headerText.Contains("manhanvien") || 
                        headerText.Contains("username"))
                    {
                        colTaiKhoan = col;
                    }
                    else if (headerText.Contains("họ tên") || headerText.Contains("hoten") || 
                             headerText.Contains("họ và tên") || headerText.Contains("fullname") || 
                             headerText == "tên")
                    {
                        colHoTen = col;
                    }
                    else if (headerText.Contains("email") || headerText.Contains("thư điện tử"))
                    {
                        colEmail = col;
                    }
                    else if (headerText.Contains("số điện thoại") || headerText.Contains("sodienthoai") || 
                             headerText.Contains("sđt") || headerText.Contains("điện thoại") || 
                             headerText.Contains("phone"))
                    {
                        colSoDienThoai = col;
                    }
                    else if (headerText.Contains("khoa/phòng") || headerText.Contains("khoaphong") || 
                             headerText.Contains("khoa phòng") || headerText.Contains("khoa") || 
                             headerText.Contains("phòng") || headerText.Contains("department"))
                    {
                        colKhoaPhong = col;
                    }
                    else if (headerText.Contains("mật khẩu") || headerText.Contains("matkhau") || 
                             headerText.Contains("password"))
                    {
                        colMatKhau = col;
                    }
                    else if (headerText.Contains("vai trò") || headerText.Contains("vaitro") || 
                             headerText.Contains("role"))
                    {
                        colVaiTro = col;
                    }
                    else if (headerText.Contains("chức danh") || headerText.Contains("chucdanh") || 
                             headerText.Contains("title"))
                    {
                        colChucDanh = col;
                    }
                }

                // Fallbacks matching the new standard layout if headers are not detected
                if (colTaiKhoan == -1) colTaiKhoan = 2;
                if (colHoTen == -1) colHoTen = 3;
                if (colEmail == -1) colEmail = 4;
                if (colSoDienThoai == -1) colSoDienThoai = 5;
                if (colKhoaPhong == -1) colKhoaPhong = 6;

                var processedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int row = 2; row <= lastRow; row++)
                {
                    var maNhanVien = colTaiKhoan > 0 ? ws.Cell(row, colTaiKhoan).GetString().Trim() : string.Empty;
                    var matKhau = colMatKhau > 0 ? ws.Cell(row, colMatKhau).GetString().Trim() : string.Empty;
                    var hoTen = colHoTen > 0 ? ws.Cell(row, colHoTen).GetString().Trim() : string.Empty;
                    var chucDanh = colChucDanh > 0 ? ws.Cell(row, colChucDanh).GetString().Trim() : string.Empty;
                    var khoaPhong = colKhoaPhong > 0 ? ws.Cell(row, colKhoaPhong).GetString().Trim() : string.Empty;
                    var vaiTroStr = colVaiTro > 0 ? ws.Cell(row, colVaiTro).GetString().Trim() : string.Empty;
                    var soDienThoai = colSoDienThoai > 0 ? ws.Cell(row, colSoDienThoai).GetString().Trim() : string.Empty;
                    var email = colEmail > 0 ? ws.Cell(row, colEmail).GetString().Trim() : string.Empty;

                    // If all columns are empty, skip row
                    if (string.IsNullOrWhiteSpace(maNhanVien) &&
                        string.IsNullOrWhiteSpace(matKhau) &&
                        string.IsNullOrWhiteSpace(hoTen) &&
                        string.IsNullOrWhiteSpace(chucDanh) &&
                        string.IsNullOrWhiteSpace(khoaPhong) &&
                        string.IsNullOrWhiteSpace(vaiTroStr) &&
                        string.IsNullOrWhiteSpace(soDienThoai) &&
                        string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    // Default password to "123456" if not provided
                    if (string.IsNullOrWhiteSpace(matKhau))
                    {
                        matKhau = "123456";
                    }

                    // Validations
                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(maNhanVien))
                    {
                        rowErrors.Add("Mã nhân viên (tài khoản) không được để trống");
                    }
                    if (string.IsNullOrWhiteSpace(matKhau))
                    {
                        rowErrors.Add("Mật khẩu không được để trống");
                    }
                    else if (matKhau.Length < 6)
                    {
                        rowErrors.Add("Mật khẩu phải từ 6 ký tự trở lên");
                    }
                    if (string.IsNullOrWhiteSpace(hoTen))
                    {
                        rowErrors.Add("Họ tên không được để trống");
                    }

                    if (rowErrors.Any())
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: {string.Join(", ", rowErrors)}");
                        continue;
                    }

                    if (processedUsernames.Contains(maNhanVien))
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: Tài khoản '{maNhanVien}' bị trùng lặp trong file import");
                        continue;
                    }

                    // Check if maNhanVien / TenDangNhap already exists
                    var existingUserByUsername = await _context.Taikhoans.FirstOrDefaultAsync(u => u.TenDangNhap == maNhanVien);
                    var existingUserByEmpCode = await _context.Taikhoans.FirstOrDefaultAsync(u => u.MaNhanVien == maNhanVien);
                    if (existingUserByUsername != null || existingUserByEmpCode != null)
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: Mã nhân viên '{maNhanVien}' đã tồn tại trong hệ thống");
                        continue;
                    }

                    // Determine role ID
                    int idVaiTro = 3; // Default is Student (Thí sinh)
                    if (!string.IsNullOrWhiteSpace(vaiTroStr))
                    {
                        if (vaiTroStr == "1")
                        {
                            idVaiTro = 1; // Admin
                        }
                        else if (vaiTroStr == "2")
                        {
                            idVaiTro = 5; // DeptManager
                        }
                        else if (vaiTroStr == "3")
                        {
                            idVaiTro = 3; // Student
                        }
                        else
                        {
                            resultDto.Failed++;
                            resultDto.Errors.Add($"Dòng {row}: Vai trò '{vaiTroStr}' không hợp lệ (chỉ chấp nhận 1, 2, 3)");
                            continue;
                        }
                    }

                    // Set chucDanh to null/empty if left empty
                    string? finalChucDanh = string.IsNullOrWhiteSpace(chucDanh) ? null : chucDanh;
                    string? finalKhoaPhong = string.IsNullOrWhiteSpace(khoaPhong) ? null : khoaPhong;

                    var user = new Taikhoan
                    {
                        TenDangNhap = maNhanVien,
                        MaNhanVien = maNhanVien,
                        MatKhau = _passwordService.HashPassword(matKhau),
                        HoTen = hoTen,
                        ChucDanh = finalChucDanh,
                        KhoaPhong = finalKhoaPhong,
                        IdVaiTro = idVaiTro,
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        SoDienThoai = string.IsNullOrWhiteSpace(soDienThoai) ? null : soDienThoai,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email
                    };

                    _context.Taikhoans.Add(user);
                    processedUsernames.Add(maNhanVien);
                    resultDto.Success++;
                }

                if (resultDto.Success > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return new BaseResponseDto<ExcelImportResultDto>
                {
                    Success = true,
                    Message = $"Import hoàn tất. Thành công: {resultDto.Success}, Thất bại: {resultDto.Failed}",
                    Data = resultDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users from Excel");
                return new BaseResponseDto<ExcelImportResultDto>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi xử lý file: " + ex.Message,
                    Data = resultDto
                };
            }
        }
    }
}