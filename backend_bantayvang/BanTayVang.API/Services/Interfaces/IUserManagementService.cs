using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;

namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Service for managing users (admin operations)
    /// </summary>
    public interface IUserManagementService
    {
        Task<BaseResponseDto<List<UserDto>>> GetAllUsersAsync(UserFilterDto filter);
        Task<BaseResponseDto<UserDto>> GetUserByIdAsync(int id);
        Task<BaseResponseDto<UserDto>> CreateUserAsync(CreateUserDto createDto);
        Task<BaseResponseDto<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateDto);
        Task<BaseResponseDto> DeactivateUserAsync(int id);
        Task<BaseResponseDto> ActivateUserAsync(int id);
        Task<BaseResponseDto> ResetUserPasswordAsync(int id, string newPassword);
        Task<BaseResponseDto> DeleteUserAsync(int id);
    }
}