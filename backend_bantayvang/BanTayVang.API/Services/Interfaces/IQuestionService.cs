using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<BaseResponseDto<PagedResultDto<QuestionDto>>> GetFilteredQuestionsAsync(QuestionFilterDto filter);
        Task<BaseResponseDto<QuestionDto>> GetQuestionByIdAsync(int id);
        Task<BaseResponseDto<QuestionDto>> CreateQuestionAsync(CreateQuestionDto createDto, int createdBy);
        Task<BaseResponseDto<QuestionDto>> UpdateQuestionAsync(UpdateQuestionDto updateDto, int updatedBy);
        Task<BaseResponseDto> DeleteQuestionAsync(int id, int updatedBy);
        Task<BaseResponseDto<List<QuestionDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int createdBy, string department, int questionCategoryId, bool isExamImport = false, int? expectedCount = null);
        Task<BaseResponseDto<List<QuestionDto>>> PreviewQuestionsFromExcelAsync(IFormFile file, int createdBy, string department, int questionCategoryId);
        Task<BaseResponseDto<List<QuestionDto>>> ImportQuestionsFromWordAsync(IFormFile file, int createdBy, string department, int questionCategoryId);
        Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync(int questionCategoryId, bool isExamImport = false);
        Task<BaseResponseDto<byte[]>> DownloadWordTemplateAsync();
        Task<BaseResponseDto<List<QuestionDto>>> GetRandomQuestionsAsync(int count, int? categoryId = null);
        Task<bool> CheckDuplicateAsync(string content, string? department = null, int? excludeId = null);
    }
}