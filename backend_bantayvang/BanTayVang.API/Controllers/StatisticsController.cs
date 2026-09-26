using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Statistics;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Statistics & Dashboard controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly BanTayVang.API.Models.BanTayVangDbContext _context;

        public StatisticsController(IStatisticsService statisticsService, BanTayVang.API.Models.BanTayVangDbContext context)
        {
            _statisticsService = statisticsService;
            _context = context;
        }

        /// <summary>
        /// Tổng quan dashboard - thống kê toàn hệ thống
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto<DashboardDto>>> GetDashboard()
        {
            var result = await _statisticsService.GetDashboardAsync();
            return Ok(result);
        }

        /// <summary>
        /// Thống kê chi tiết của một kỳ thi
        /// </summary>
        [HttpGet("exam-campaign/{examCampaignId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<ExamStatisticsDto>>> GetExamStatistics(int examCampaignId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartmentId = DepartmentAuthHelper.GetDeptManagerDepartmentId(User);
                // BUG FIX: `campaign.DepartmentId != myDepartmentId` is false when BOTH are null -
                // a DeptManager with no managed_department_id could view statistics for any
                // "shared" campaign (DepartmentId=null, i.e. not scoped to one department).
                // myDepartmentId == null must always fail closed regardless of the campaign.
                // A campaign can now be scoped to 1-n departments (ExamCampaignDepartments) instead
                // of one - check membership there instead of the old single DepartmentId column.
                var campaignExists = await _context.ExamCampaigns.AnyAsync(k => k.Id == examCampaignId);
                var isLinkedToMyDept = myDepartmentId != null && await _context.Set<BanTayVang.API.Models.ExamCampaignDepartment>()
                    .AnyAsync(kd => kd.ExamCampaignId == examCampaignId && kd.DepartmentId == myDepartmentId.Value);
                if (!campaignExists || myDepartmentId == null || !isLinkedToMyDept)
                {
                    return Forbid();
                }
            }
            var result = await _statisticsService.GetExamStatisticsAsync(examCampaignId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lịch sử thi của user
        /// </summary>
        [HttpGet("user/{userId}/history")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<UserExamHistoryDto>>>> GetUserHistory(int userId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null || targetUser.Department != myDepartment)
                {
                    return Forbid();
                }
            }
            var result = await _statisticsService.GetUserExamHistoryAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lịch sử thi của user hiện tại (đang đăng nhập)
        /// </summary>
        [HttpGet("my-history")]
        public async Task<ActionResult<BaseResponseDto<List<UserExamHistoryDto>>>> GetMyHistory()
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _statisticsService.GetUserExamHistoryAsync(userId.Value, applyPublishGate: true);
            return Ok(result);
        }

        /// <summary>
        /// Top performers (xếp hạng theo điểm trung bình)
        /// </summary>
        [HttpGet("top-performers")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto<List<TopPerformerDto>>>> GetTopPerformers([FromQuery] int top = 10)
        {
            var result = await _statisticsService.GetTopPerformersAsync(top);
            return Ok(result);
        }
    }
}