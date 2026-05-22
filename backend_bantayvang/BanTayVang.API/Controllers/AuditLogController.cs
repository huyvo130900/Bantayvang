using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Audit Log controller for admin/supervisor
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Lấy log gần nhất (top 200)
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetRecent([FromQuery] int top = 200)
        {
            var logs = await _auditLogService.GetRecentLogsAsync(top);
            return Ok(new BaseResponseDto<List<AuditLogEntry>>
            {
                Success = true,
                Message = "Thành công",
                Data = logs
            });
        }

        /// <summary>
        /// Lấy log của 1 user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByUser(int userId, [FromQuery] int top = 100)
        {
            var logs = await _auditLogService.GetUserLogsAsync(userId, top);
            return Ok(new BaseResponseDto<List<AuditLogEntry>>
            {
                Success = true,
                Message = "Thành công",
                Data = logs
            });
        }

        /// <summary>
        /// Lấy log của 1 bài thi
        /// </summary>
        [HttpGet("exam-session/{baithiId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByExamSession(int baithiId)
        {
            var logs = await _auditLogService.GetExamSessionLogsAsync(baithiId);
            return Ok(new BaseResponseDto<List<AuditLogEntry>>
            {
                Success = true,
                Message = "Thành công",
                Data = logs
            });
        }

        /// <summary>
        /// Tìm kiếm log
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> Search(
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var logs = await _auditLogService.SearchLogsAsync(actionType, from, to);
            return Ok(new BaseResponseDto<List<AuditLogEntry>>
            {
                Success = true,
                Message = "Thành công",
                Data = logs
            });
        }
    }
}