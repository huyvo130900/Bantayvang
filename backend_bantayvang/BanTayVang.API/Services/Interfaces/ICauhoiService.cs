using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;

namespace BanTayVang.API.Services.Interfaces
{
    public interface ICauhoiService
    {
        Task<BaseResponseDto<PagedResultDto<CauhoiDto>>> GetFilteredQuestionsAsync(QuestionFilterDto filter);
        Task<BaseResponseDto<CauhoiDto>> GetQuestionByIdAsync(int id);
        Task<BaseResponseDto<CauhoiDto>> CreateQuestionAsync(CreateCauhoiDto createDto, int nguoiTao);
        Task<BaseResponseDto<CauhoiDto>> UpdateQuestionAsync(UpdateCauhoiDto updateDto, int nguoiCapNhat);
        Task<BaseResponseDto> DeleteQuestionAsync(int id, int nguoiCapNhat);
        Task<BaseResponseDto<List<CauhoiDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int nguoiTao);
        Task<BaseResponseDto<List<CauhoiDto>>> GetRandomQuestionsAsync(int count, int? danhMucId = null);
    }
}