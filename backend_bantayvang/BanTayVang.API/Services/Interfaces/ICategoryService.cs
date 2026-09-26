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
        Task<BaseResponseDto<List<QuestionCategoryDto>>> GetAllQuestionTypesAsync();
        Task<BaseResponseDto<QuestionCategoryDto>> GetQuestionTypeByIdAsync(int id);
        Task<BaseResponseDto<QuestionCategoryDto>> CreateQuestionTypeAsync(CreateQuestionCategoryDto createDto);
        Task<BaseResponseDto<QuestionCategoryDto>> UpdateQuestionTypeAsync(int id, CreateQuestionCategoryDto updateDto);
        Task<BaseResponseDto> DeleteQuestionTypeAsync(int id);
    }
}
