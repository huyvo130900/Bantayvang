using BanTayVang.API.DTOs.Category;
using BanTayVang.API.DTOs.Common;

namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Service for managing question categories (Danh muc) and types (Loai cau hoi)
    /// Follows SRP - focused on category management
    /// </summary>
    public interface ICategoryService
    {
        // Question type (Loai cau hoi) operations
        Task<BaseResponseDto<List<LoaicauhoiDto>>> GetAllQuestionTypesAsync();
        Task<BaseResponseDto<LoaicauhoiDto>> GetQuestionTypeByIdAsync(int id);
        Task<BaseResponseDto<LoaicauhoiDto>> CreateQuestionTypeAsync(CreateLoaicauhoiDto createDto);
        Task<BaseResponseDto<LoaicauhoiDto>> UpdateQuestionTypeAsync(int id, CreateLoaicauhoiDto updateDto);
        Task<BaseResponseDto> DeleteQuestionTypeAsync(int id);
    }
}