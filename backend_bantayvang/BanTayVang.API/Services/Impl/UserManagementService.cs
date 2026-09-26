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

                // BUG FIX: Skip/Take with no OrderBy has no guaranteed row order in SQL Server, so
                // page 2+ could show duplicate or missing rows compared to page 1. Order by Id for
                // a stable, deterministic sort.
                var pagedUsers = query
                    .OrderBy(u => u.Id)
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

                // BUG FIX: CreateUserDto.Password had no length/strength constraint at all (unlike
                // RegisterDto), and this method never called ValidatePasswordStrength - an
                // admin/DeptManager could create a user with e.g. a 1-character password. Enforce
                // the same rule used for self-registration and password resets. (Deliberately NOT
                // applied to the bulk Excel/CSV import path, which keeps its own default-password
                // behavior for empty rows - see ImportUsersFromExcelAsync.)
                var passwordValidation = _passwordService.ValidatePasswordStrength(createDto.Password);
                if (!passwordValidation.IsValid)
                {
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Mật khẩu không đủ mạnh", Errors = passwordValidation.Errors };
                }

                // BUG FIX: this path (single-user creation) never required DeptManagerDeptId for
                // RoleId=5, unlike the bulk Excel/CSV import path which explicitly rejects such rows
                // (see ImportUsersFromExcelAsync below). Calling this API directly - or any future
                // caller of it - could create a DeptManager account with no department, and every
                // department-scoping check across the app (ExamRegistrationController.Approve/Reject,
                // ExamService.GetActiveMonitorListAsync, ExamSubmissionService.ForceSubmitAsync, ...)
                // reads managed_department_id from the JWT and treats an empty value as "no
                // restriction" rather than "no department" - so such an account could act across
                // every department instead of none. Reject at the source instead of only patching
                // every consumer.
                if (createDto.RoleId == 5 && !createDto.DeptManagerDeptId.HasValue)
                {
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Vai trò Quản lý khoa bắt buộc phải chọn Khoa/Phòng quản lý." };
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
                    CreatedAt = DateTime.UtcNow.AddHours(7),
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
                        khoa.UpdatedAt = DateTime.UtcNow.AddHours(7);
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

                // BUG FIX: same gap as CreateUserAsync above - updating a user to RoleId=5 (or
                // editing an existing DeptManager) without a DeptManagerDeptId was silently allowed.
                if (updateDto.RoleId == 5 && !updateDto.DeptManagerDeptId.HasValue)
                {
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Vai trò Quản lý khoa bắt buộc phải chọn Khoa/Phòng quản lý." };
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
                        oldKhoa.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                        khoa.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);

                // Clear department manager mapping if they are being deactivated/deleted
                if (user.RoleId == 5 && user.DeptManagerDeptId.HasValue)
                {
                    var khoa = await _context.Departments.FindAsync(user.DeptManagerDeptId.Value);
                    if (khoa != null && khoa.DeptManagerId == user.Id)
                    {
                        khoa.DeptManagerId = null;
                        khoa.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                // BUG FIX: only checked length >= 6, unlike every other password-setting path
                // (self-registration, change-password, reset-via-OTP) which run the full
                // ValidatePasswordStrength check.
                var passwordValidation = _passwordService.ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsValid)
                    return new BaseResponseDto { Success = false, Message = "Mật khẩu không đủ mạnh", Errors = passwordValidation.Errors };

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy người dùng" };

                user.Password = _passwordService.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                        khoa.UpdatedAt = DateTime.UtcNow.AddHours(7);
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

                        -- Xóa chi tiết làm bài thi
                        DELETE FROM SubmissionDetails WHERE ExamSubmissionId IN (SELECT Id FROM ExamSubmissions WHERE UserId = {0});
                        -- Xóa bài thi
                        DELETE FROM ExamSubmissions WHERE UserId = {0};
                        -- Gỡ trưởng khoa (chuyển NULL) để Departments không mồ côi quản lý
                        UPDATE Departments SET DeptManagerId = NULL WHERE DeptManagerId = {0};
                        -- Gỡ người duyệt trong ExamRegistrations (ApproverId không có FK constraint nhưng cần set NULL để tránh orphan data)
                        UPDATE ExamRegistrations SET ApproverId = NULL WHERE ApproverId = {0};
                        -- Xóa phân công thi
                        DELETE FROM ExamAssignments WHERE UserId = {0};
                        -- Xóa tài khoản vai trò
                        DELETE FROM UserRoles WHERE UserId = {0};
                        -- Xóa các bảng liên quan phiên đăng nhập, JWT, Thông báo
                        DELETE FROM LoginSessions WHERE UserId = {0};
                        DELETE FROM UserSessions WHERE UserId = {0};
                        DELETE FROM RefreshTokens WHERE UserId = {0};
                        DELETE FROM Notifications WHERE UserId = {0};
                        DELETE FROM AuditLogs WHERE UserId = {0};

                        -- Xóa tài khoản chính
                        DELETE FROM Users WHERE Id = {0};

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

        public async Task<BaseResponseDto> BulkDeleteUsersAsync(List<int> ids, string? restrictToDepartment = null)
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
                int skippedOtherDept = 0;
                foreach (var user in targetUsers)
                {
                    // Skip admin
                    if (user.RoleId == 1) continue;

                    // BUG FIX: DeptManager callers must only be able to affect users in their own
                    // department - mirrors CanAccessDepartment used by every other per-user action.
                    if (!string.IsNullOrEmpty(restrictToDepartment) && user.Department != restrictToDepartment)
                    {
                        skippedOtherDept++;
                        continue;
                    }

                    user.IsDeleted = true;
                    user.Status = false; // Cũng vô hiệu hóa luôn
                    count++;
                }

                if (count == 0 && skippedOtherDept > 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Bạn chỉ được phép xóa người dùng thuộc khoa của mình" };
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

        public async Task<BaseResponseDto> BulkHardDeleteUsersAsync(List<int> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Không có người dùng nào được chọn" };
                }

                int count = 0;
                var errors = new List<string>();

                foreach (var id in ids)
                {
                    // Reuse HardDeleteUserAsync which has its own transaction and admin validation
                    var res = await HardDeleteUserAsync(id);
                    if (res.Success)
                    {
                        count++;
                    }
                    else
                    {
                        errors.Add($"ID {id}: {res.Message}");
                    }
                }

                if (count == 0)
                {
                    return new BaseResponseDto { Success = false, Message = "Không thể xóa vĩnh viễn tài khoản nào. Chi tiết: " + string.Join(", ", errors) };
                }

                string msg = $"Đã xóa vĩnh viễn {count} tài khoản thành công.";
                if (errors.Any())
                {
                    msg += $" Có {errors.Count} tài khoản không thể xóa.";
                }

                return new BaseResponseDto { Success = true, Message = msg };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk hard deleting users");
                return new BaseResponseDto { Success = false, Message = "Lỗi khi xóa vĩnh viễn hàng loạt: " + ex.Message };
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
                var rawRows = new List<(int Row, string EmployeeCode, string Password, string FullName, string JobTitle, string Department, string RoleStr, string PhoneNumber, string Email)>();

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

                    int colEmployeeCode = -1, colFullName = -1, colEmail = -1, colPhoneNumber = -1, colDepartment = -1;
                    int colPassword = -1, colRole = -1, colJobTitle = -1;

                    if (headerRecord != null)
                    {
                        for (int col = 0; col < headerRecord.Length; col++)
                        {
                            var rawHeaderText = headerRecord[col] ?? string.Empty;
                            var headerText = RemoveSign4Vietnamese(rawHeaderText).ToLowerInvariant();
                            if (string.IsNullOrEmpty(headerText)) continue;

                            if (headerText.Contains("tai khoan") || headerText.Contains("username") || 
                                headerText.Contains("ten dang nhap") || headerText.Contains("ma nhan vien"))
                                colEmployeeCode = col;
                            else if (headerText.Contains("ho ten") || headerText.Contains("fullname") || 
                                     headerText.Contains("ho va ten") || headerText == "ten")
                                colFullName = col;
                            else if (headerText.Contains("email") || headerText.Contains("thu dien tu"))
                                colEmail = col;
                            else if (headerText.Contains("so dien thoai") || headerText.Contains("sdt") || 
                                     headerText.Contains("dien thoai") || headerText.Contains("phone"))
                                colPhoneNumber = col;
                            else if (headerText.Contains("khoa/phong") || headerText.Contains("khoaphong") || 
                                     headerText.Contains("khoa phong") || headerText.Contains("khoa") || 
                                     headerText.Contains("phong") || headerText.Contains("department"))
                                colDepartment = col;
                            else if (headerText.Contains("mat khau") || headerText.Contains("password"))
                                colPassword = col;
                            else if (headerText.Contains("vai tro") || headerText.Contains("role"))
                                colRole = col;
                            else if (headerText.Contains("chuc danh") || headerText.Contains("title"))
                                colJobTitle = col;
                        }
                    }

                    if (colEmployeeCode == -1) colEmployeeCode = 1;
                    if (colFullName == -1) colFullName = 2;
                    if (colEmail == -1) colEmail = 3;
                    if (colPhoneNumber == -1) colPhoneNumber = 4;
                    if (colDepartment == -1) colDepartment = 5;

                    int row = 1;
                    while (csv.Read())
                    {
                        row++;
                        rawRows.Add((
                            Row: row,
                            EmployeeCode: colEmployeeCode >= 0 && colEmployeeCode < csv.Parser.Count ? csv.GetField(colEmployeeCode)?.Trim() ?? string.Empty : string.Empty,
                            Password: colPassword >= 0 && colPassword < csv.Parser.Count ? csv.GetField(colPassword)?.Trim() ?? string.Empty : string.Empty,
                            FullName: colFullName >= 0 && colFullName < csv.Parser.Count ? csv.GetField(colFullName)?.Trim() ?? string.Empty : string.Empty,
                            JobTitle: colJobTitle >= 0 && colJobTitle < csv.Parser.Count ? csv.GetField(colJobTitle)?.Trim() ?? string.Empty : string.Empty,
                            Department: colDepartment >= 0 && colDepartment < csv.Parser.Count ? csv.GetField(colDepartment)?.Trim() ?? string.Empty : string.Empty,
                            RoleStr: colRole >= 0 && colRole < csv.Parser.Count ? csv.GetField(colRole)?.Trim() ?? string.Empty : string.Empty,
                            PhoneNumber: colPhoneNumber >= 0 && colPhoneNumber < csv.Parser.Count ? csv.GetField(colPhoneNumber)?.Trim() ?? string.Empty : string.Empty,
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

                    int colEmployeeCode = -1, colFullName = -1, colEmail = -1, colPhoneNumber = -1, colDepartment = -1;
                    int colPassword = -1, colRole = -1, colJobTitle = -1;

                    var firstRow = ws.Row(1);
                    int lastCell = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 8;
                    bool isSingleColumnCsvInExcel = lastCell == 1 && firstRow.Cell(1).GetString().Contains(",");

                    if (isSingleColumnCsvInExcel)
                    {
                        var headers = firstRow.Cell(1).GetString().Split(',');
                        for (int col = 0; col < headers.Length; col++)
                        {
                            var rawHeaderText = headers[col];
                            var headerText = RemoveSign4Vietnamese(rawHeaderText).ToLowerInvariant();
                            if (string.IsNullOrEmpty(headerText)) continue;

                            if (headerText.Contains("tai khoan") || headerText.Contains("username") || 
                                headerText.Contains("ten dang nhap") || headerText.Contains("ma nhan vien"))
                                colEmployeeCode = col + 1;
                            else if (headerText.Contains("ho ten") || headerText.Contains("fullname") || 
                                     headerText.Contains("ho va ten") || headerText == "ten")
                                colFullName = col + 1;
                            else if (headerText.Contains("email") || headerText.Contains("thu dien tu"))
                                colEmail = col + 1;
                            else if (headerText.Contains("so dien thoai") || headerText.Contains("sdt") || 
                                     headerText.Contains("dien thoai") || headerText.Contains("phone"))
                                colPhoneNumber = col + 1;
                            else if (headerText.Contains("khoa/phong") || headerText.Contains("khoaphong") || 
                                     headerText.Contains("khoa phong") || headerText.Contains("khoa") || 
                                     headerText.Contains("phong") || headerText.Contains("department"))
                                colDepartment = col + 1;
                            else if (headerText.Contains("mat khau") || headerText.Contains("password"))
                                colPassword = col + 1;
                            else if (headerText.Contains("vai tro") || headerText.Contains("role"))
                                colRole = col + 1;
                            else if (headerText.Contains("chuc danh") || headerText.Contains("title"))
                                colJobTitle = col + 1;
                        }
                    }
                    else
                    {
                        for (int col = 1; col <= lastCell; col++)
                        {
                            var rawHeaderText = firstRow.Cell(col).GetString();
                            var headerText = RemoveSign4Vietnamese(rawHeaderText).ToLowerInvariant();
                            if (string.IsNullOrEmpty(headerText)) continue;

                            if (headerText.Contains("tai khoan") || headerText.Contains("username") || 
                                headerText.Contains("ten dang nhap") || headerText.Contains("ma nhan vien"))
                                colEmployeeCode = col;
                            else if (headerText.Contains("ho ten") || headerText.Contains("fullname") || 
                                     headerText.Contains("ho va ten") || headerText == "ten")
                                colFullName = col;
                            else if (headerText.Contains("email") || headerText.Contains("thu dien tu"))
                                colEmail = col;
                            else if (headerText.Contains("so dien thoai") || headerText.Contains("sdt") || 
                                     headerText.Contains("dien thoai") || headerText.Contains("phone"))
                                colPhoneNumber = col;
                            else if (headerText.Contains("khoa/phong") || headerText.Contains("khoaphong") || 
                                     headerText.Contains("khoa phong") || headerText.Contains("khoa") || 
                                     headerText.Contains("phong") || headerText.Contains("department"))
                                colDepartment = col;
                            else if (headerText.Contains("mat khau") || headerText.Contains("password"))
                                colPassword = col;
                            else if (headerText.Contains("vai tro") || headerText.Contains("role"))
                                colRole = col;
                            else if (headerText.Contains("chuc danh") || headerText.Contains("title"))
                                colJobTitle = col;
                        }
                    }

                    if (colEmployeeCode == -1) colEmployeeCode = 2;
                    if (colFullName == -1) colFullName = 3;
                    if (colEmail == -1) colEmail = 4;
                    if (colPhoneNumber == -1) colPhoneNumber = 5;
                    if (colDepartment == -1) colDepartment = 6;

                    for (int row = 2; row <= lastRow; row++)
                    {
                        if (isSingleColumnCsvInExcel)
                        {
                            var parts = ws.Cell(row, 1).GetString().Split(',');
                            rawRows.Add((
                                Row: row,
                                EmployeeCode: colEmployeeCode > 0 && colEmployeeCode <= parts.Length ? parts[colEmployeeCode - 1].Trim() : string.Empty,
                                Password: colPassword > 0 && colPassword <= parts.Length ? parts[colPassword - 1].Trim() : string.Empty,
                                FullName: colFullName > 0 && colFullName <= parts.Length ? parts[colFullName - 1].Trim() : string.Empty,
                                JobTitle: colJobTitle > 0 && colJobTitle <= parts.Length ? parts[colJobTitle - 1].Trim() : string.Empty,
                                Department: colDepartment > 0 && colDepartment <= parts.Length ? parts[colDepartment - 1].Trim() : string.Empty,
                                RoleStr: colRole > 0 && colRole <= parts.Length ? parts[colRole - 1].Trim() : string.Empty,
                                PhoneNumber: colPhoneNumber > 0 && colPhoneNumber <= parts.Length ? parts[colPhoneNumber - 1].Trim() : string.Empty,
                                Email: colEmail > 0 && colEmail <= parts.Length ? parts[colEmail - 1].Trim() : string.Empty
                            ));
                        }
                        else
                        {
                            rawRows.Add((
                                Row: row,
                                EmployeeCode: colEmployeeCode > 0 ? ws.Cell(row, colEmployeeCode).GetString().Trim() : string.Empty,
                                Password: colPassword > 0 ? ws.Cell(row, colPassword).GetString().Trim() : string.Empty,
                                FullName: colFullName > 0 ? ws.Cell(row, colFullName).GetString().Trim() : string.Empty,
                                JobTitle: colJobTitle > 0 ? ws.Cell(row, colJobTitle).GetString().Trim() : string.Empty,
                                Department: colDepartment > 0 ? ws.Cell(row, colDepartment).GetString().Trim() : string.Empty,
                                RoleStr: colRole > 0 ? ws.Cell(row, colRole).GetString().Trim() : string.Empty,
                                PhoneNumber: colPhoneNumber > 0 ? ws.Cell(row, colPhoneNumber).GetString().Trim() : string.Empty,
                                Email: colEmail > 0 ? ws.Cell(row, colEmail).GetString().Trim() : string.Empty
                            ));
                        }
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
                    var roleStr = r.RoleStr;
                    var phoneNumber = r.PhoneNumber;
                    var email = r.Email;

                    // If all columns are empty, skip row
                    if (string.IsNullOrWhiteSpace(employeeCode) &&
                        string.IsNullOrWhiteSpace(password) &&
                        string.IsNullOrWhiteSpace(fullName) &&
                        string.IsNullOrWhiteSpace(jobTitle) &&
                        string.IsNullOrWhiteSpace(department) &&
                        string.IsNullOrWhiteSpace(roleStr) &&
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
                    if (!string.IsNullOrWhiteSpace(roleStr))
                    {
                        var normalizedRole = RemoveSign4Vietnamese(roleStr).ToLowerInvariant();
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
                            if (normalizedRole.Contains("ngoai")) roleId = 6;
                            else roleId = 3; // Student
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

                    // BUG FIX: the single-user create/edit dialog requires deptManagerDeptId
                    // whenever roleId is DeptManager (see schemas.ts), but this bulk-import path
                    // never enforced the equivalent - a row with role "Quản lý" and an empty
                    // "Khoa/Phòng" column silently created a DeptManager with Department=null and
                    // DeptManagerDeptId=null. Every DeptManager-scoped check across the app reads
                    // that claim from the JWT and treats an empty value as "no restriction" rather
                    // than "no department" (see AuditLogController fix) - so such an account could
                    // see/act on data across every department instead of none. Reject the row
                    // instead of silently creating a broken/over-privileged account.
                    if (roleId == 5 && string.IsNullOrEmpty(finalKhoaPhong))
                    {
                        resultDto.Failed++;
                        resultDto.Errors.Add($"Dòng {row}: Mã nhân viên '{employeeCode}' có vai trò Quản lý khoa nhưng thiếu cột Khoa/Phòng - bắt buộc phải có khoa quản lý");
                        continue;
                    }

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
                        CreatedAt = DateTime.UtcNow.AddHours(7),
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

                if (resultDto.Failed > 0)
                {
                    return new BaseResponseDto<ExcelImportResultDto>
                    {
                        Success = false,
                        Message = $"File Excel có {resultDto.Failed} dòng lỗi. Không có tài khoản nào được thêm mới. Vui lòng sửa lỗi và import lại từ đầu.",
                        Data = resultDto,
                        Errors = resultDto.Errors
                    };
                }

                if (resultDto.Success > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return new BaseResponseDto<ExcelImportResultDto>
                {
                    Success = true,
                    Message = $"Import hoàn tất. Đã thêm thành công {resultDto.Success} tài khoản.",
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
