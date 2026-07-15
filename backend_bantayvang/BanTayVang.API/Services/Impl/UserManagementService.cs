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
                var query = filter.IncludeDeleted 
                    ? _context.Taikhoans.IgnoreQueryFilters().Where(u => u.IsDeleted).AsQueryable() 
                    : _context.Taikhoans.AsQueryable();

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
                    .Include(u => u.KhoaQuanLy)
                    .ToList()
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
                var user = await _context.Taikhoans.IgnoreQueryFilters().Include(u => u.KhoaQuanLy).FirstOrDefaultAsync(u => u.Id == id);
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
                var existing = await _context.Taikhoans.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.TenDangNhap == createDto.TenDangNhap || u.MaNhanVien == createDto.TenDangNhap);
                if (existing != null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Tên đăng nhập đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };

                if (!string.IsNullOrWhiteSpace(createDto.MaNhanVien))
                {
                    var existingByEmpCode = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.MaNhanVien == createDto.MaNhanVien);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };
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

                // Add to role mapping table to maintain database integrity
                user.TaikhoanVaitros.Add(new TaikhoanVaitro
                {
                    IdVaiTro = createDto.IdVaiTro
                });

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
                    var existingByEmpCode = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.MaNhanVien == updateDto.MaNhanVien && u.Id != id);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };
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

                // Update role mapping if role changed
                if (user.IdVaiTro != updateDto.IdVaiTro)
                {
                    var oldRoleMappings = _context.TaikhoanVaitros.Where(tv => tv.IdTaiKhoan == user.Id);
                    _context.TaikhoanVaitros.RemoveRange(oldRoleMappings);

                    user.TaikhoanVaitros.Add(new TaikhoanVaitro
                    {
                        IdVaiTro = updateDto.IdVaiTro
                    });
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
                // 1. Xác nhận user tồn tại trước khi làm bất cứ điều gì
                var target = await _userRepository.GetByIdAsync(id);
                if (target == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };
                }

                // 2. Không cho xóa Admin cuối cùng của hệ thống (tránh khóa quyền truy cập vĩnh viễn)
                if (target.IdVaiTro == 1)
                {
                    var admins = await _userRepository.GetByRoleAsync(1);
                    var otherAdminsCount = admins.Count(a => a.Id != id);
                    if (otherAdminsCount == 0)
                    {
                        return new BaseResponseDto { Success = false, Message = "Không thể xóa Admin cuối cùng trong hệ thống" };
                    }
                }

                // 3. Nếu là DeptManager, giải phóng liên kết khôa/phòng trước khi xóa mềm
                if (target.IdVaiTro == 5 && target.IdKhoaQuanLy.HasValue)
                {
                    var khoa = await _context.KhoaPhongs.FindAsync(target.IdKhoaQuanLy.Value);
                    if (khoa != null && khoa.DeptManagerId == target.Id)
                    {
                        khoa.DeptManagerId = null;
                        khoa.NgayCapNhat = DateTime.Now;
                    }
                    target.IdKhoaQuanLy = null;
                }

                // 4. Thực hiện Soft Delete
                target.IsDeleted = true;
                target.TrangThai = false;

                // Xóa token đăng nhập qua DbContext (chưa SaveChanges)
                var tokens = _context.RefreshTokens.Where(t => t.UserId == id);
                _context.RefreshTokens.RemoveRange(tokens);
                var sessions = _context.UserSessions.Where(s => s.UserId == id);
                _context.UserSessions.RemoveRange(sessions);
                var loginSessions = _context.Phiendangnhaps.Where(s => s.IdTaiKhoan == id);
                _context.Phiendangnhaps.RemoveRange(loginSessions);

                // Update trực tiếp qua DbContext để chỉ cần 1 lần SaveChanges
                _context.Taikhoans.Update(target);
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã đưa người dùng vào thùng rác" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return new BaseResponseDto { Success = false, Message = "Có lỗi xảy ra khi xóa người dùng: " + ex.Message };
            }
        }

        public async Task<BaseResponseDto> RestoreUserAsync(int id)
        {
            try
            {
                var target = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (target == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };
                }

                target.IsDeleted = false;
                target.TrangThai = true;
                await _userRepository.UpdateAsync(target);

                return new BaseResponseDto { Success = true, Message = "Đã khôi phục người dùng thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user {UserId}", id);
                return new BaseResponseDto { Success = false, Message = "Có lỗi xảy ra khi khôi phục người dùng: " + ex.Message };
            }
        }

        public async Task<BaseResponseDto> HardDeleteUserAsync(int id)
        {
            try
            {
                var target = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (target == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };
                }

                if (target.IdVaiTro == 1)
                {
                    var otherAdminsCount = await _context.Taikhoans.IgnoreQueryFilters().CountAsync(a => a.IdVaiTro == 1 && a.Id != id);
                    if (otherAdminsCount == 0)
                    {
                        return new BaseResponseDto { Success = false, Message = "Không thể xóa Admin cuối cùng trong hệ thống" };
                    }
                }

                // 3. Sử dụng raw SQL để xóa tận gốc các dữ liệu liên kết trước (Cascade Delete bằng tay)
                // Các bảng như LOGTHAOTAC có thể cấu hình SET_NULL nhưng xóa sạch cho an toàn.
                // SET XACT_ABORT ON + TRY/CATCH/ROLLBACK đảm bảo toàn bộ script là 1 khối atomic:
                // nếu bất kỳ câu lệnh nào lỗi (vd: vướng 1 FK chưa lường tới), mọi thay đổi trước đó
                // sẽ được rollback thay vì bị COMMIT dở dang gây "mồ côi" dữ liệu.
                string sql = @"
                    SET XACT_ABORT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;

                        -- Xóa cảnh báo gian lận và log liên quan đến bài thi
                        DELETE FROM CANHBAOGIANLAN WHERE IdBaiThi IN (SELECT Id FROM BAITHI WHERE IdTaiKhoan = {0});
                        DELETE FROM LOGTHAOTAC WHERE IdBaiThi IN (SELECT Id FROM BAITHI WHERE IdTaiKhoan = {0});

                        -- Xóa lịch sử thi (anti-cheat), có FK NOT NULL tới TAIKHOAN - nếu bảng tồn tại
                        IF OBJECT_ID('dbo.LICHSU_THI', 'U') IS NOT NULL
                            DELETE FROM LICHSU_THI WHERE IdThiSinh = {0};

                        -- Xóa chi tiết làm bài thi
                        DELETE FROM CHITIETLAMBAI WHERE IdBaiThi IN (SELECT Id FROM BAITHI WHERE IdTaiKhoan = {0});
                        -- Xóa bài thi
                        DELETE FROM BAITHI WHERE IdTaiKhoan = {0};
                        -- Gỡ trưởng khoa (chuyển NULL) để KHOA_PHONG không mồ côi quản lý
                        UPDATE KHOA_PHONG SET DeptManagerId = NULL WHERE DeptManagerId = {0};
                        -- Gỡ người duyệt trong DANG_KY_THI (NguoiDuyetId không có FK constraint nhưng cần set NULL để tránh orphan data)
                        UPDATE DANG_KY_THI SET NguoiDuyetId = NULL WHERE NguoiDuyetId = {0};
                        -- Xóa phân công thi
                        DELETE FROM PHANCONG_THI WHERE UserId = {0};
                        -- Xóa tài khoản vai trò
                        DELETE FROM TAIKHOAN_VAITRO WHERE IdTaiKhoan = {0};
                        -- Xóa các bảng liên quan phiên đăng nhập, JWT, Thông báo
                        DELETE FROM PHIENDANGNHAP WHERE IdTaiKhoan = {0};
                        DELETE FROM PHIEN_NGUOIDUNG WHERE UserId = {0};
                        DELETE FROM TOKEN_LAM_MOI WHERE UserId = {0};
                        DELETE FROM THONGBAO WHERE UserId = {0};
                        DELETE FROM LOGTHAOTAC WHERE IdTaiKhoan = {0};

                        -- Xóa tài khoản chính
                        DELETE FROM TAIKHOAN WHERE Id = {0};

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                ";

                await _context.Database.ExecuteSqlRawAsync(sql, id);

                return new BaseResponseDto { Success = true, Message = "Đã xóa vĩnh viễn người dùng và các dữ liệu liên quan" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return new BaseResponseDto { Success = false, Message = "Có lỗi xảy ra khi xóa vĩnh viễn người dùng: " + ex.Message };
            }
        }

        public async Task<BaseResponseDto> BulkDeleteUsersAsync(List<int> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Không có người dùng nào được chọn" };
                }

                var targetUsers = await _context.Taikhoans.Where(t => ids.Contains(t.Id)).ToListAsync();
                if (targetUsers.Count == 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng nào hợp lệ" };
                }

                int count = 0;
                foreach (var user in targetUsers)
                {
                    // Skip admin
                    if (user.IdVaiTro == 1) continue;

                    user.IsDeleted = true;
                    user.TrangThai = false; // Cũng vô hiệu hóa luôn
                    count++;
                }

                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = $"Đã đưa {count} tài khoản vào Thùng rác thành công." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting users");
                return new BaseResponseDto { Success = false, Message = "Lỗi khi xóa hàng loạt: " + ex.Message };
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
                IsDeleted = u.IsDeleted,
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
            6 => "ThiSinhNgoai",
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
                var rawRows = new List<(int Row, string MaNhanVien, string MatKhau, string HoTen, string ChucDanh, string KhoaPhong, string VaiTroStr, string SoDienThoai, string Email)>();

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

                    int colTaiKhoan = -1, colHoTen = -1, colEmail = -1, colSoDienThoai = -1, colKhoaPhong = -1;
                    int colMatKhau = -1, colVaiTro = -1, colChucDanh = -1;

                    if (headerRecord != null)
                    {
                        for (int col = 0; col < headerRecord.Length; col++)
                        {
                            var rawHeaderText = headerRecord[col] ?? string.Empty;
                            var headerText = RemoveSign4Vietnamese(rawHeaderText).ToLowerInvariant();
                            if (string.IsNullOrEmpty(headerText)) continue;

                            if (headerText.Contains("tai khoan") || headerText.Contains("username") || 
                                headerText.Contains("ten dang nhap") || headerText.Contains("ma nhan vien"))
                                colTaiKhoan = col;
                            else if (headerText.Contains("ho ten") || headerText.Contains("fullname") || 
                                     headerText.Contains("ho va ten") || headerText == "ten")
                                colHoTen = col;
                            else if (headerText.Contains("email") || headerText.Contains("thu dien tu"))
                                colEmail = col;
                            else if (headerText.Contains("so dien thoai") || headerText.Contains("sdt") || 
                                     headerText.Contains("dien thoai") || headerText.Contains("phone"))
                                colSoDienThoai = col;
                            else if (headerText.Contains("khoa/phong") || headerText.Contains("khoaphong") || 
                                     headerText.Contains("khoa phong") || headerText.Contains("khoa") || 
                                     headerText.Contains("phong") || headerText.Contains("department"))
                                colKhoaPhong = col;
                            else if (headerText.Contains("mat khau") || headerText.Contains("password"))
                                colMatKhau = col;
                            else if (headerText.Contains("vai tro") || headerText.Contains("role"))
                                colVaiTro = col;
                            else if (headerText.Contains("chuc danh") || headerText.Contains("title"))
                                colChucDanh = col;
                        }
                    }

                    if (colTaiKhoan == -1) colTaiKhoan = 1;
                    if (colHoTen == -1) colHoTen = 2;
                    if (colEmail == -1) colEmail = 3;
                    if (colSoDienThoai == -1) colSoDienThoai = 4;
                    if (colKhoaPhong == -1) colKhoaPhong = 5;

                    int row = 1;
                    while (csv.Read())
                    {
                        row++;
                        rawRows.Add((
                            Row: row,
                            MaNhanVien: colTaiKhoan >= 0 && colTaiKhoan < csv.Parser.Count ? csv.GetField(colTaiKhoan)?.Trim() ?? string.Empty : string.Empty,
                            MatKhau: colMatKhau >= 0 && colMatKhau < csv.Parser.Count ? csv.GetField(colMatKhau)?.Trim() ?? string.Empty : string.Empty,
                            HoTen: colHoTen >= 0 && colHoTen < csv.Parser.Count ? csv.GetField(colHoTen)?.Trim() ?? string.Empty : string.Empty,
                            ChucDanh: colChucDanh >= 0 && colChucDanh < csv.Parser.Count ? csv.GetField(colChucDanh)?.Trim() ?? string.Empty : string.Empty,
                            KhoaPhong: colKhoaPhong >= 0 && colKhoaPhong < csv.Parser.Count ? csv.GetField(colKhoaPhong)?.Trim() ?? string.Empty : string.Empty,
                            VaiTroStr: colVaiTro >= 0 && colVaiTro < csv.Parser.Count ? csv.GetField(colVaiTro)?.Trim() ?? string.Empty : string.Empty,
                            SoDienThoai: colSoDienThoai >= 0 && colSoDienThoai < csv.Parser.Count ? csv.GetField(colSoDienThoai)?.Trim() ?? string.Empty : string.Empty,
                            Email: colEmail >= 0 && colEmail < csv.Parser.Count ? csv.GetField(colEmail)?.Trim() ?? string.Empty : string.Empty
                        ));
                    }
                }
                else
                {
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets
                        .FirstOrDefault(w => w.Name.Contains("IMPORT") || w.Name.Contains("TAI_KHOAN") || w.Name.Contains("USER"))
                        ?? workbook.Worksheets.First();

                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                    int colTaiKhoan = -1, colHoTen = -1, colEmail = -1, colSoDienThoai = -1, colKhoaPhong = -1;
                    int colMatKhau = -1, colVaiTro = -1, colChucDanh = -1;

                    var firstRow = ws.Row(1);
                    int lastCell = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 8;
                    for (int col = 1; col <= lastCell; col++)
                    {
                        var rawHeaderText = firstRow.Cell(col).GetString();
                        var headerText = RemoveSign4Vietnamese(rawHeaderText).ToLowerInvariant();
                        if (string.IsNullOrEmpty(headerText)) continue;

                        if (headerText.Contains("tai khoan") || headerText.Contains("username") || 
                            headerText.Contains("ten dang nhap") || headerText.Contains("ma nhan vien"))
                            colTaiKhoan = col;
                        else if (headerText.Contains("ho ten") || headerText.Contains("fullname") || 
                                 headerText.Contains("ho va ten") || headerText == "ten")
                            colHoTen = col;
                        else if (headerText.Contains("email") || headerText.Contains("thu dien tu"))
                            colEmail = col;
                        else if (headerText.Contains("so dien thoai") || headerText.Contains("sdt") || 
                                 headerText.Contains("dien thoai") || headerText.Contains("phone"))
                            colSoDienThoai = col;
                        else if (headerText.Contains("khoa/phong") || headerText.Contains("khoaphong") || 
                                 headerText.Contains("khoa phong") || headerText.Contains("khoa") || 
                                 headerText.Contains("phong") || headerText.Contains("department"))
                            colKhoaPhong = col;
                        else if (headerText.Contains("mat khau") || headerText.Contains("password"))
                            colMatKhau = col;
                        else if (headerText.Contains("vai tro") || headerText.Contains("role"))
                            colVaiTro = col;
                        else if (headerText.Contains("chuc danh") || headerText.Contains("title"))
                            colChucDanh = col;
                    }

                    if (colTaiKhoan == -1) colTaiKhoan = 2;
                    if (colHoTen == -1) colHoTen = 3;
                    if (colEmail == -1) colEmail = 4;
                    if (colSoDienThoai == -1) colSoDienThoai = 5;
                    if (colKhoaPhong == -1) colKhoaPhong = 6;

                    for (int row = 2; row <= lastRow; row++)
                    {
                        rawRows.Add((
                            Row: row,
                            MaNhanVien: colTaiKhoan > 0 ? ws.Cell(row, colTaiKhoan).GetString().Trim() : string.Empty,
                            MatKhau: colMatKhau > 0 ? ws.Cell(row, colMatKhau).GetString().Trim() : string.Empty,
                            HoTen: colHoTen > 0 ? ws.Cell(row, colHoTen).GetString().Trim() : string.Empty,
                            ChucDanh: colChucDanh > 0 ? ws.Cell(row, colChucDanh).GetString().Trim() : string.Empty,
                            KhoaPhong: colKhoaPhong > 0 ? ws.Cell(row, colKhoaPhong).GetString().Trim() : string.Empty,
                            VaiTroStr: colVaiTro > 0 ? ws.Cell(row, colVaiTro).GetString().Trim() : string.Empty,
                            SoDienThoai: colSoDienThoai > 0 ? ws.Cell(row, colSoDienThoai).GetString().Trim() : string.Empty,
                            Email: colEmail > 0 ? ws.Cell(row, colEmail).GetString().Trim() : string.Empty
                        ));
                    }
                }

                var processedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in rawRows)
                {
                    int row = r.Row;
                    var maNhanVien = r.MaNhanVien;
                    var matKhau = r.MatKhau;
                    var hoTen = r.HoTen;
                    var chucDanh = r.ChucDanh;
                    var khoaPhong = r.KhoaPhong;
                    var vaiTroStr = r.VaiTroStr;
                    var soDienThoai = r.SoDienThoai;
                    var email = r.Email;

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
                    var existingUserByUsername = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenDangNhap == maNhanVien);
                    var existingUserByEmpCode = await _context.Taikhoans.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.MaNhanVien == maNhanVien);
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
                        var normalizedRole = RemoveSign4Vietnamese(vaiTroStr).ToLowerInvariant();
                        if (normalizedRole == "1" || normalizedRole.Contains("quan tri") || normalizedRole.Contains("admin"))
                        {
                            idVaiTro = 1; // Admin
                        }
                        else if (normalizedRole == "2" || normalizedRole.Contains("quan ly") || normalizedRole.Contains("dept") || normalizedRole.Contains("manager"))
                        {
                            idVaiTro = 5; // DeptManager
                        }
                        else if (normalizedRole == "3" || normalizedRole.Contains("thi sinh") || normalizedRole.Contains("student") || normalizedRole.Contains("hoc vien") || normalizedRole.Contains("sinh vien"))
                        {
                            idVaiTro = 3; // Student
                        }
                        else
                        {
                            // Unrecognized role defaults to Student (Thí sinh) as requested
                            idVaiTro = 3;
                        }
                    }

                    // Set chucDanh to null/empty if left empty
                    string? finalChucDanh = string.IsNullOrWhiteSpace(chucDanh) ? null : chucDanh;
                    string? finalKhoaPhong = string.IsNullOrWhiteSpace(khoaPhong) ? null : khoaPhong;

                    // Auto-assign DeptManager to KhoaPhong
                    int? idKhoaQuanLy = null;
                    if (idVaiTro == 5 && !string.IsNullOrEmpty(finalKhoaPhong))
                    {
                        var khoa = await _context.KhoaPhongs.FirstOrDefaultAsync(k => k.TenKhoa == finalKhoaPhong);
                        if (khoa != null)
                        {
                            idKhoaQuanLy = khoa.Id;
                        }
                    }

                    var user = new Taikhoan
                    {
                        TenDangNhap = maNhanVien,
                        MaNhanVien = maNhanVien,
                        MatKhau = _passwordService.HashPassword(matKhau),
                        HoTen = hoTen,
                        ChucDanh = finalChucDanh,
                        KhoaPhong = finalKhoaPhong,
                        IdVaiTro = idVaiTro,
                        IdKhoaQuanLy = idKhoaQuanLy,
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        SoDienThoai = string.IsNullOrWhiteSpace(soDienThoai) ? null : soDienThoai,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email
                    };

                    // Add to role mapping table to maintain database integrity
                    user.TaikhoanVaitros.Add(new TaikhoanVaitro
                    {
                        IdVaiTro = idVaiTro
                    });

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

        private static string RemoveSign4Vietnamese(string utf8String)
        {
            if (string.IsNullOrWhiteSpace(utf8String)) return string.Empty;

            // Normalize to FormD (decomposed) so that diacritics are separated
            string formD = utf8String.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (char ch in formD)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                // Keep only non-diacritic characters
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            // Replace 'đ' and 'Đ' manually as they are not standard Unicode diacritics
            string result = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
            result = result.Replace('đ', 'd').Replace('Đ', 'D');
            return result.Trim();
        }
    }
}