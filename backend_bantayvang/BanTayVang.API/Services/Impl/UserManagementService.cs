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
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            IUserRepository userRepository,
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
                    ? _context.Users.IgnoreQueryFilters().Where(u => u.IsDeleted).AsQueryable() 
                    : _context.Users.AsQueryable();

                if (filter.RoleId.HasValue)
                    query = query.Where(u => u.RoleId == filter.RoleId);

                if (filter.Status.HasValue)
                    query = query.Where(u => u.Status == filter.Status);

                if (!string.IsNullOrEmpty(filter.Department))
                    query = query.Where(u => u.Department == filter.Department);

                if (!string.IsNullOrEmpty(filter.SearchKeyword))
                {
                    var keyword = filter.SearchKeyword.ToLower();
                    query = query.Where(u => 
                        (u.Username ?? "").ToLower().Contains(keyword) ||
                        (u.FullName ?? "").ToLower().Contains(keyword));
                }

                var pagedUsers = query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Include(u => u.ManagedDepartment)
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
                var user = await _context.Users.IgnoreQueryFilters().Include(u => u.ManagedDepartment).FirstOrDefaultAsync(u => u.Id == id);
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
                var existing = await _context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Username == createDto.Username || u.EmployeeCode == createDto.Username);
                if (existing != null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Tên đăng nhập đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };

                if (!string.IsNullOrWhiteSpace(createDto.EmployeeCode))
                {
                    var existingByEmpCode = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.EmployeeCode == createDto.EmployeeCode);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };
                }

                // Validate department manager assignment
                if (createDto.RoleId == 5 && createDto.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(createDto.DeptManagerDeptId.Value);
                    if (khoa != null && khoa.DeptManagerId.HasValue)
                    {
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Khoa này đã có người quản lý. Không thể tạo thêm." };
                    }
                }

                var user = new User
                {
                    Username = createDto.Username,
                    Password = _passwordService.HashPassword(createDto.Password),
                    FullName = createDto.FullName,
                    EmployeeCode = createDto.EmployeeCode,
                    JobTitle = createDto.JobTitle,
                    Department = createDto.Department,
                    RoleId = createDto.RoleId,
                    Status = createDto.Status,
                    CreatedAt = DateTime.Now,
                    DeptManagerDeptId = createDto.RoleId == 5 ? createDto.DeptManagerDeptId : null,
                    Email = createDto.Email,
                    PhoneNumber = createDto.PhoneNumber
                };

                // Add to role mapping table to maintain database integrity
                user.UserRoles.Add(new UserRole
                {
                    RoleId = createDto.RoleId
                });

                var saved = await _userRepository.AddAsync(user);

                // Auto-assign DeptManager to Department and sync Department string from Khoa name
                if (createDto.RoleId == 5 && createDto.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(createDto.DeptManagerDeptId.Value);
                    if (khoa != null)
                    {
                        // Update DeptManagerId on the department
                        khoa.DeptManagerId = saved.Id;
                        khoa.UpdatedAt = DateTime.Now;
                        // Sync Department string on user for JWT claim
                        saved.Department = khoa.DepartmentName;
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

                if (!string.IsNullOrWhiteSpace(updateDto.EmployeeCode))
                {
                    var existingByEmpCode = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.EmployeeCode == updateDto.EmployeeCode && u.Id != id);
                    if (existingByEmpCode != null)
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Mã nhân viên đã tồn tại trong hệ thống (bao gồm cả thùng rác)" };
                }

                // Validate new department manager assignment before proceeding
                if (updateDto.RoleId == 5 && updateDto.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(updateDto.DeptManagerDeptId.Value);
                    if (khoa != null && khoa.DeptManagerId.HasValue && khoa.DeptManagerId.Value != user.Id)
                    {
                        return new BaseResponseDto<UserDto> { Success = false, Message = "Khoa này đã có người quản lý. Không thể gán thêm." };
                    }
                }

                // Clear old department manager mapping if they were previously managing a department
                if (user.RoleId == 5 && user.DeptManagerDeptId.HasValue)
                {
                    var oldKhoa = await _context.Departments.FindAsync(user.DeptManagerDeptId.Value);
                    if (oldKhoa != null && oldKhoa.DeptManagerId == user.Id)
                    {
                        oldKhoa.DeptManagerId = null;
                        oldKhoa.UpdatedAt = DateTime.Now;
                    }
                }

                // Update role mapping if role changed
                if (user.RoleId != updateDto.RoleId)
                {
                    var oldRoleMappings = _context.UserRoles.Where(tv => tv.UserId == user.Id);
                    _context.UserRoles.RemoveRange(oldRoleMappings);

                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = updateDto.RoleId
                    });
                }

                user.FullName = updateDto.FullName;
                user.EmployeeCode = updateDto.EmployeeCode;
                user.JobTitle = updateDto.JobTitle;
                user.Department = updateDto.Department;
                user.RoleId = updateDto.RoleId;
                user.Status = updateDto.Status;
                user.UpdatedAt = DateTime.Now;
                user.Email = updateDto.Email;
                user.PhoneNumber = updateDto.PhoneNumber;

                // Handle new department manager assignment
                if (updateDto.RoleId == 5 && updateDto.DeptManagerDeptId.HasValue)
                {
                    user.DeptManagerDeptId = updateDto.DeptManagerDeptId.Value;
                    var khoa = await _context.Departments.FindAsync(updateDto.DeptManagerDeptId.Value);
                    if (khoa != null)
                    {
                        khoa.DeptManagerId = user.Id;
                        khoa.UpdatedAt = DateTime.Now;
                        // Sync string description for JWT claim
                        user.Department = khoa.DepartmentName;
                    }
                }
                else
                {
                    user.DeptManagerDeptId = null;
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

                user.Status = false;
                user.UpdatedAt = DateTime.Now;

                // Clear department manager mapping if they are being deactivated/deleted
                if (user.RoleId == 5 && user.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(user.DeptManagerDeptId.Value);
                    if (khoa != null && khoa.DeptManagerId == user.Id)
                    {
                        khoa.DeptManagerId = null;
                        khoa.UpdatedAt = DateTime.Now;
                    }
                    user.DeptManagerDeptId = null;
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

                user.Status = true;
                user.UpdatedAt = DateTime.Now;
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

                user.Password = _passwordService.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
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
                if (target.RoleId == 1)
                {
                    var admins = await _userRepository.GetByRoleAsync(1);
                    var otherAdminsCount = admins.Count(a => a.Id != id);
                    if (otherAdminsCount == 0)
                    {
                        return new BaseResponseDto { Success = false, Message = "Không thể xóa Admin cuối cùng trong hệ thống" };
                    }
                }

                // 3. Nếu là DeptManager, giải phóng liên kết khôa/phòng trước khi xóa mềm
                if (target.RoleId == 5 && target.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(target.DeptManagerDeptId.Value);
                    if (khoa != null && khoa.DeptManagerId == target.Id)
                    {
                        khoa.DeptManagerId = null;
                        khoa.UpdatedAt = DateTime.Now;
                    }
                    target.DeptManagerDeptId = null;
                }

                // 4. Thực hiện Soft Delete
                target.IsDeleted = true;
                target.Status = false;

                // Xóa token đăng nhập qua DbContext (chưa SaveChanges)
                var tokens = _context.RefreshTokens.Where(t => t.UserId == id);
                _context.RefreshTokens.RemoveRange(tokens);
                var sessions = _context.UserSessions.Where(s => s.UserId == id);
                _context.UserSessions.RemoveRange(sessions);
                var loginSessions = _context.LoginSessions.Where(s => s.UserId == id);
                _context.LoginSessions.RemoveRange(loginSessions);

                // Update trực tiếp qua DbContext để chỉ cần 1 lần SaveChanges
                _context.Users.Update(target);
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
                var target = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (target == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };
                }

                target.IsDeleted = false;
                target.Status = true;
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
                var target = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (target == null)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };
                }

                if (target.RoleId == 1)
                {
                    var otherAdminsCount = await _context.Users.IgnoreQueryFilters().CountAsync(a => a.RoleId == 1 && a.Id != id);
                    if (otherAdminsCount == 0)
                    {
                        return new BaseResponseDto { Success = false, Message = "Không thể xóa Admin cuối cùng trong hệ thống" };
                    }
                }

                // 3. Sử dụng raw SQL để xóa tận gốc các dữ liệu liên kết trước (Cascade Delete bằng tay)
                // Các bảng như AuditLogs có thể cấu hình SET_NULL nhưng xóa sạch cho an toàn.
                // SET XACT_ABORT ON + TRY/CATCH/ROLLBACK đảm bảo toàn bộ script là 1 khối atomic:
                // nếu bất kỳ câu lệnh nào lỗi (vd: vướng 1 FK chưa lường tới), mọi thay đổi trước đó
                // sẽ được rollback thay vì bị COMMIT dở dang gây "mồ côi" dữ liệu.
                string sql = @"
                    SET XACT_ABORT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;

                        -- Xóa cảnh báo gian lận và log liên quan đến bài thi
                        DELETE FROM CheatWarnings WHERE ExamSubmissionId IN (SELECT Id FROM ExamSubmissions WHERE UserId = {0});
                        DELETE FROM AuditLogs WHERE ExamSubmissionId IN (SELECT Id FROM ExamSubmissions WHERE UserId = {0});

                        -- Xóa lịch sử thi (anti-cheat), có FK NOT NULL tới USER - nếu bảng tồn tại
                        IF OBJECT_ID('dbo.LICHSU_THI', 'U') IS NOT NULL
                            DELETE FROM LICHSU_THI WHERE IdThiSinh = {0};

                        -- Xóa chi tiết làm bài thi
                        DELETE FROM SubmissionDetails WHERE ExamSubmissionId IN (SELECT Id FROM ExamSubmissions WHERE UserId = {0});
                        -- Xóa bài thi
                        DELETE FROM ExamSubmissions WHERE UserId = {0};
                        -- Gỡ trưởng khoa (chuyển NULL) để ExamRegistrations không mồ côi quản lý
                        UPDATE ExamRegistrations SET DeptManagerId = NULL WHERE DeptManagerId = {0};
                        -- Gỡ người duyệt trong DANG_KY_THI (NguoiDuyetId không có FK constraint nhưng cần set NULL để tránh orphan data)
                        UPDATE DANG_KY_THI SET NguoiDuyetId = NULL WHERE NguoiDuyetId = {0};
                        -- Xóa phân công thi
                        DELETE FROM PHANCONG_THI WHERE UserId = {0};
                        -- Xóa tài khoản vai trò
                        DELETE FROM TAIKHOAN_VAITRO WHERE UserId = {0};
                        -- Xóa các bảng liên quan phiên đăng nhập, JWT, Thông báo
                        DELETE FROM PHIENDANGNHAP WHERE UserId = {0};
                        DELETE FROM PHIEN_NGUOIDUNG WHERE UserId = {0};
                        DELETE FROM TOKEN_LAM_MOI WHERE UserId = {0};
                        DELETE FROM THONGBAO WHERE UserId = {0};
                        DELETE FROM AuditLogs WHERE UserId = {0};

                        -- Xóa tài khoản chính
                        DELETE FROM USER WHERE Id = {0};

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

                var targetUsers = await _context.Users.Where(t => ids.Contains(t.Id)).ToListAsync();
                if (targetUsers.Count == 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng nào hợp lệ" };
                }

                int count = 0;
                foreach (var user in targetUsers)
                {
                    // Skip admin
                    if (user.RoleId == 1) continue;

                    user.IsDeleted = true;
                    user.Status = false; // Cũng vô hiệu hóa luôn
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

        private static UserDto MapToDto(User u)
        {
            return new UserDto
            {
                Id = u.Id,
                EmployeeCode = u.EmployeeCode,
                Username = u.Username,
                FullName = u.FullName,
                JobTitle = u.JobTitle,
                Department = u.Department,
                RoleId = u.RoleId,
                RoleName = GetRoleName(u.RoleId),
                DeptManagerDeptId = u.DeptManagerDeptId,
                DeptManagerDeptName = u.ManagedDepartment?.DepartmentName,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsDeleted = u.IsDeleted,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber
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
                var rawRows = new List<(int Row, string EmployeeCode, string Password, string FullName, string JobTitle, string Department, string VaiTroStr, string PhoneNumber, string Email)>();

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
                            EmployeeCode: colTaiKhoan >= 0 && colTaiKhoan < csv.Parser.Count ? csv.GetField(colTaiKhoan)?.Trim() ?? string.Empty : string.Empty,
                            Password: colMatKhau >= 0 && colMatKhau < csv.Parser.Count ? csv.GetField(colMatKhau)?.Trim() ?? string.Empty : string.Empty,
                            FullName: colHoTen >= 0 && colHoTen < csv.Parser.Count ? csv.GetField(colHoTen)?.Trim() ?? string.Empty : string.Empty,
                            JobTitle: colChucDanh >= 0 && colChucDanh < csv.Parser.Count ? csv.GetField(colChucDanh)?.Trim() ?? string.Empty : string.Empty,
                            Department: colKhoaPhong >= 0 && colKhoaPhong < csv.Parser.Count ? csv.GetField(colKhoaPhong)?.Trim() ?? string.Empty : string.Empty,
                            VaiTroStr: colVaiTro >= 0 && colVaiTro < csv.Parser.Count ? csv.GetField(colVaiTro)?.Trim() ?? string.Empty : string.Empty,
                            PhoneNumber: colSoDienThoai >= 0 && colSoDienThoai < csv.Parser.Count ? csv.GetField(colSoDienThoai)?.Trim() ?? string.Empty : string.Empty,
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
                            EmployeeCode: colTaiKhoan > 0 ? ws.Cell(row, colTaiKhoan).GetString().Trim() : string.Empty,
                            Password: colMatKhau > 0 ? ws.Cell(row, colMatKhau).GetString().Trim() : string.Empty,
                            FullName: colHoTen > 0 ? ws.Cell(row, colHoTen).GetString().Trim() : string.Empty,
                            JobTitle: colChucDanh > 0 ? ws.Cell(row, colChucDanh).GetString().Trim() : string.Empty,
                            Department: colKhoaPhong > 0 ? ws.Cell(row, colKhoaPhong).GetString().Trim() : string.Empty,
                            VaiTroStr: colVaiTro > 0 ? ws.Cell(row, colVaiTro).GetString().Trim() : string.Empty,
                            PhoneNumber: colSoDienThoai > 0 ? ws.Cell(row, colSoDienThoai).GetString().Trim() : string.Empty,
                            Email: colEmail > 0 ? ws.Cell(row, colEmail).GetString().Trim() : string.Empty
                        ));
                    }
                }

                var processedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in rawRows)
                {
                    int row = r.Row;
                    var employeeCode = r.EmployeeCode;
                    var password = r.Password;
                    var fullName = r.FullName;
                    var jobTitle = r.JobTitle;
                    var department = r.Department;
                    var vaiTroStr = r.VaiTroStr;
                    var phoneNumber = r.PhoneNumber;
                    var email = r.Email;

                    // If all columns are empty, skip row
                    if (string.IsNullOrWhiteSpace(employeeCode) &&
                        string.IsNullOrWhiteSpace(password) &&
                        string.IsNullOrWhiteSpace(fullName) &&
                        string.IsNullOrWhiteSpace(jobTitle) &&
                        string.IsNullOrWhiteSpace(department) &&
                        string.IsNullOrWhiteSpace(vaiTroStr) &&
                        string.IsNullOrWhiteSpace(phoneNumber) &&
                        string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    // Default password to "123456" if not provided
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        password = "123456";
                    }

                    // Validations
                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(employeeCode))
                    {
                        rowErrors.Add("Mã nhân viên (tài khoản) không được để trống");
                    }
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        rowErrors.Add("Mật khẩu không được để trống");
                    }
                    else if (password.Length < 6)
                    {
                        rowErrors.Add("Mật khẩu phải từ 6 ký tự trở lên");
                    }
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        rowErrors.Add("Họ tên không được để trống");
                    }

                    if (rowErrors.Any())
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: {string.Join(", ", rowErrors)}");
                        continue;
                    }

                    if (processedUsernames.Contains(employeeCode))
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: Tài khoản '{employeeCode}' bị trùng lặp trong file import");
                        continue;
                    }

                    // Check if employeeCode / Username already exists
                    var existingUserByUsername = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == employeeCode);
                    var existingUserByEmpCode = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.EmployeeCode == employeeCode);
                    if (existingUserByUsername != null || existingUserByEmpCode != null)
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: Mã nhân viên '{employeeCode}' đã tồn tại trong hệ thống");
                        continue;
                    }

                    // Determine role ID
                    int roleId = 3; // Default is Student (Thí sinh)
                    if (!string.IsNullOrWhiteSpace(vaiTroStr))
                    {
                        var normalizedRole = RemoveSign4Vietnamese(vaiTroStr).ToLowerInvariant();
                        if (normalizedRole == "1" || normalizedRole.Contains("quan tri") || normalizedRole.Contains("admin"))
                        {
                            roleId = 1; // Admin
                        }
                        else if (normalizedRole == "2" || normalizedRole.Contains("quan ly") || normalizedRole.Contains("dept") || normalizedRole.Contains("manager"))
                        {
                            roleId = 5; // DeptManager
                        }
                        else if (normalizedRole == "3" || normalizedRole.Contains("thi sinh") || normalizedRole.Contains("student") || normalizedRole.Contains("hoc vien") || normalizedRole.Contains("sinh vien"))
                        {
                            roleId = 3; // Student
                        }
                        else
                        {
                            // Unrecognized role defaults to Student (Thí sinh) as requested
                            roleId = 3;
                        }
                    }

                    // Set jobTitle to null/empty if left empty
                    string? finalChucDanh = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
                    string? finalKhoaPhong = string.IsNullOrWhiteSpace(department) ? null : department;

                    // Auto-assign DeptManager to Department
                    int? deptManagerDeptId = null;
                    if (roleId == 5 && !string.IsNullOrEmpty(finalKhoaPhong))
                    {
                        var khoa = await _context.Departments.FirstOrDefaultAsync(k => k.DepartmentName == finalKhoaPhong);
                        if (khoa != null)
                        {
                            deptManagerDeptId = khoa.Id;
                        }
                    }

                    var user = new User
                    {
                        Username = employeeCode,
                        EmployeeCode = employeeCode,
                        Password = _passwordService.HashPassword(password),
                        FullName = fullName,
                        JobTitle = finalChucDanh,
                        Department = finalKhoaPhong,
                        RoleId = roleId,
                        DeptManagerDeptId = deptManagerDeptId,
                        Status = true,
                        CreatedAt = DateTime.Now,
                        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email
                    };

                    // Add to role mapping table to maintain database integrity
                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = roleId
                    });

                    _context.Users.Add(user);
                    processedUsernames.Add(employeeCode);
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