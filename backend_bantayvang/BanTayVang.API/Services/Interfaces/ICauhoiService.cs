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
        Task<BaseResponseDto<List<CauhoiDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int nguoiTao, string khoaPhong, int idLoaiCauHoi, bool isExamImport = false, int? expectedCount = null);
        Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync(int idLoaiCauHoi, bool isExamImport = false);
        Task<BaseResponseDto<List<CauhoiDto>>> GetRandomQuestionsAsync(int count);
        Task<bool> CheckDuplicateAsync(string noiDung, string? khoaPhong = null, int? excludeId = null);
    }
}