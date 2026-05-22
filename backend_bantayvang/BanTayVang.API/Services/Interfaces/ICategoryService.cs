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
        // Category (Danh muc) operations
        Task<BaseResponseDto<List<DanhmucauhoiDto>>> GetAllCategoriesAsync();
        Task<BaseResponseDto<DanhmucauhoiDto>> GetCategoryByIdAsync(int id);
        Task<BaseResponseDto<DanhmucauhoiDto>> CreateCategoryAsync(CreateDanhmucauhoiDto createDto);
        Task<BaseResponseDto<DanhmucauhoiDto>> UpdateCategoryAsync(int id, CreateDanhmucauhoiDto updateDto);
        Task<BaseResponseDto> DeleteCategoryAsync(int id);

        // Question type (Loai cau hoi) operations
        Task<BaseResponseDto<List<LoaicauhoiDto>>> GetAllQuestionTypesAsync();
        Task<BaseResponseDto<LoaicauhoiDto>> GetQuestionTypeByIdAsync(int id);
        Task<BaseResponseDto<LoaicauhoiDto>> CreateQuestionTypeAsync(CreateLoaicauhoiDto createDto);
        Task<BaseResponseDto<LoaicauhoiDto>> UpdateQuestionTypeAsync(int id, CreateLoaicauhoiDto updateDto);
        Task<BaseResponseDto> DeleteQuestionTypeAsync(int id);
    }
}