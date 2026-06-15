using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.KyThi;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IKyThiService
    {
        Task<BaseResponseDto<List<KyThiDto>>> GetAllAsync(string? trangThai = null, int? currentUserId = null);
        Task<BaseResponseDto<KyThiDto>> GetByIdAsync(int id);
        Task<BaseResponseDto<KyThiDto>> CreateAsync(CreateKyThiDto dto, int nguoiTao);
        Task<BaseResponseDto<KyThiDto>> UpdateAsync(int id, UpdateKyThiDto dto);
        Task<BaseResponseDto> UpdateStatusAsync(int id, string trangThai);
        Task<BaseResponseDto> DeleteAsync(int id);

        // Sinh đề cho kỳ thi
        Task<BaseResponseDto<ExamCheckResultDto>> CheckExamsAvailabilityAsync(int kyThiId, ExamGenerationConfigDto config);
        Task<BaseResponseDto> GenerateExamsForKyThiAsync(int kyThiId, ExamGenerationConfigDto config, int nguoiTao);
    }
}