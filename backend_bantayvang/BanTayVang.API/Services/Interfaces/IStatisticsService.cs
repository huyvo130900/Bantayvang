using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Statistics;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<BaseResponseDto<DashboardDto>> GetDashboardAsync();
        Task<BaseResponseDto<ExamStatisticsDto>> GetExamStatisticsAsync(int kyThiId);
        Task<BaseResponseDto<List<UserExamHistoryDto>>> GetUserExamHistoryAsync(int userId);
        Task<BaseResponseDto<List<TopPerformerDto>>> GetTopPerformersAsync(int top = 10);
    }
}