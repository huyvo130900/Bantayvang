using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Statistics;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Statistics & Dashboard controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// Tổng quan dashboard - thống kê toàn hệ thống
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<BaseResponseDto<DashboardDto>>> GetDashboard()
        {
            var result = await _statisticsService.GetDashboardAsync();
            return Ok(result);
        }

        /// <summary>
        /// Thống kê chi tiết của một đề thi
        /// </summary>
        [HttpGet("exam/{examId}")]
        public async Task<ActionResult<BaseResponseDto<ExamStatisticsDto>>> GetExamStatistics(int examId)
        {
            var result = await _statisticsService.GetExamStatisticsAsync(examId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lịch sử thi của user
        /// </summary>
        [HttpGet("user/{userId}/history")]
        public async Task<ActionResult<BaseResponseDto<List<UserExamHistoryDto>>>> GetUserHistory(int userId)
        {
            var result = await _statisticsService.GetUserExamHistoryAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lịch sử thi của user hiện tại (đang đăng nhập)
        /// </summary>
        [HttpGet("my-history")]
        public async Task<ActionResult<BaseResponseDto<List<UserExamHistoryDto>>>> GetMyHistory()
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _statisticsService.GetUserExamHistoryAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Top performers (xếp hạng theo điểm trung bình)
        /// </summary>
        [HttpGet("top-performers")]
        public async Task<ActionResult<BaseResponseDto<List<TopPerformerDto>>>> GetTopPerformers([FromQuery] int top = 10)
        {
            var result = await _statisticsService.GetTopPerformersAsync(top);
            return Ok(result);
        }
    }
}