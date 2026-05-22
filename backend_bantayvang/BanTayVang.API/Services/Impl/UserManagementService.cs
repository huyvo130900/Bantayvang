using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;

namespace BanTayVang.API.Services.Impl
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ITaikhoanRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            ITaikhoanRepository userRepository,
            IPasswordService passwordService,
            ILogger<UserManagementService> logger)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
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
                    NgayTao = DateTime.Now
                };

                var saved = await _userRepository.AddAsync(user);

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

                user.Email = updateDto.Email;
                user.HoTen = updateDto.HoTen;
                user.MaNhanVien = updateDto.MaNhanVien;
                user.ChucDanh = updateDto.ChucDanh;
                user.KhoaPhong = updateDto.KhoaPhong;
                user.IdVaiTro = updateDto.IdVaiTro;
                user.TrangThai = updateDto.TrangThai;
                user.NgayCapNhat = DateTime.Now;

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
                TrangThai = u.TrangThai,
                NgayTao = u.NgayTao,
                LanDangNhapCuoi = u.LanDangNhapCuoi
            };
        }

        private static string GetRoleName(int? roleId) => roleId switch
        {
            1 => "Admin",
            2 => "Teacher",
            3 => "Student",
            4 => "Supervisor",
            _ => "Unknown"
        };
    }
}