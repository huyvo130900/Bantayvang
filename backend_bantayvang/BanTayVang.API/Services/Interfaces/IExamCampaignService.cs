using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.ExamCampaign;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamCampaignService
    {
        Task<BaseResponseDto<List<ExamCampaignDto>>> GetAllAsync(string? status = null, int? currentUserId = null);
        Task<BaseResponseDto<ExamCampaignDto>> GetByIdAsync(int id);
        Task<BaseResponseDto<ExamCampaignDto>> CreateAsync(CreateKyThiDto dto, int createdBy);
        Task<BaseResponseDto<ExamCampaignDto>> UpdateAsync(int id, UpdateKyThiDto dto);
        Task<BaseResponseDto> UpdateStatusAsync(int id, string status);
        Task<BaseResponseDto> DeleteAsync(int id);

        // Sinh đề cho kỳ thi
        Task<BaseResponseDto<ExamCheckResultDto>> CheckExamsAvailabilityAsync(int examCampaignId, ExamGenerationConfigDto config);
        Task<BaseResponseDto> GenerateExamsForCampaignAsync(int examCampaignId, ExamGenerationConfigDto config, int createdBy);
    }
}