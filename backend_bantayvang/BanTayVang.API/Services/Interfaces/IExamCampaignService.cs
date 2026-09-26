using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.ExamCampaign;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamCampaignService
    {
        Task<BaseResponseDto<List<ExamCampaignDto>>> GetAllAsync(string? status = null, int? currentUserId = null);
        Task<BaseResponseDto<ExamCampaignDto>> GetByIdAsync(int id);
        Task<BaseResponseDto<ExamCampaignDto>> CreateAsync(CreateExamCampaignDto dto, int createdBy);
        Task<BaseResponseDto<ExamCampaignDto>> UpdateAsync(int id, UpdateExamCampaignDto dto);
        Task<BaseResponseDto> UpdateStatusAsync(int id, string status);
        Task<BaseResponseDto> DeleteAsync(int id);
        Task<BaseResponseDto<List<ExamCampaignEligibilityDto>>> GetEligibilityAsync(int examCampaignId);
        Task<BaseResponseDto<AssignFromExcelResultDto>> AssignFromExcelAsync(int examCampaignId, Microsoft.AspNetCore.Http.IFormFile file);

        // Sinh đề cho kỳ thi
        Task<BaseResponseDto<ExamCheckResultDto>> CheckExamsAvailabilityAsync(int examCampaignId, ExamGenerationConfigDto config);
        Task<BaseResponseDto> GenerateExamsForCampaignAsync(int examCampaignId, ExamGenerationConfigDto config, int createdBy);
    }
}
