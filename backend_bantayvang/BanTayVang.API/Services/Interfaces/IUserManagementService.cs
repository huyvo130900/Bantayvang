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
        Task<BaseResponseDto> RestoreUserAsync(int id);
        Task<BaseResponseDto> HardDeleteUserAsync(int id);
        Task<BaseResponseDto> BulkDeleteUsersAsync(List<int> ids, string? restrictToDepartment = null);
        Task<BaseResponseDto> BulkHardDeleteUsersAsync(List<int> ids);
        Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync();
        Task<BaseResponseDto<ExcelImportResultDto>> ImportUsersFromExcelAsync(Microsoft.AspNetCore.Http.IFormFile file);
    }
}