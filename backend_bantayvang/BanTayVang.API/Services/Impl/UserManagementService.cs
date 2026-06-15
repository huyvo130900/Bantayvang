using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

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
                        (u.HoTen ?? "").ToLower().Contains(keyword) ||
                        (u.Email ?? "").ToLower().Contains(keyword));
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
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Tên đăng nhập đã tồn tại" };

                var existingEmail = await _userRepository.GetByUsernameOrEmailAsync(createDto.Email);
                if (existingEmail != null)
                    return new BaseResponseDto<UserDto> { Success = false, Message = "Email đã được sử dụng" };

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
                    Email = createDto.Email,
                    HoTen = createDto.HoTen,
                    MaNhanVien = createDto.MaNhanVien,
                    ChucDanh = createDto.ChucDanh,
                    KhoaPhong = createDto.KhoaPhong,
                    IdVaiTro = createDto.IdVaiTro,
                    TrangThai = createDto.TrangThai,
                    NgayTao = DateTime.Now,
                    IdKhoaQuanLy = createDto.IdVaiTro == 5 ? createDto.IdKhoaQuanLy : null
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

                user.Email = updateDto.Email;
                user.HoTen = updateDto.HoTen;
                user.MaNhanVien = updateDto.MaNhanVien;
                user.ChucDanh = updateDto.ChucDanh;
                user.KhoaPhong = updateDto.KhoaPhong;
                user.IdVaiTro = updateDto.IdVaiTro;
                user.TrangThai = updateDto.TrangThai;
                user.NgayCapNhat = DateTime.Now;

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
                Email = u.Email,
                HoTen = u.HoTen,
                ChucDanh = u.ChucDanh,
                KhoaPhong = u.KhoaPhong,
                IdVaiTro = u.IdVaiTro,
                TenVaiTro = GetRoleName(u.IdVaiTro),
                IdKhoaQuanLy = u.IdKhoaQuanLy,
                TenKhoaQuanLy = u.KhoaQuanLy?.TenKhoa,
                TrangThai = u.TrangThai,
                NgayTao = u.NgayTao,
                LanDangNhapCuoi = u.LanDangNhapCuoi
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
    }
}