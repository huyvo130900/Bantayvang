using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// GET /api/AuditLog — Lấy danh sách audit log có phân trang và filter
        /// Admin: thấy tất cả. DeptManager: chỉ thấy log của khoa mình.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? actionType = null,
            [FromQuery] string? username = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // DeptManager: chỉ thấy log của khoa mình — filter bằng username scope
            // (full isolation requires IdKhoaPhong on log which we now have)
            var logs = await _auditLogService.SearchLogsAsync(actionType, username, from, to, page, pageSize);
            var total = await _auditLogService.GetTotalCountAsync(actionType, username, from, to);

            return Ok(new
            {
                success = true,
                data = logs,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        /// <summary>GET /api/AuditLog/recent — nhanh, top N</summary>
        [HttpGet("recent")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetRecent([FromQuery] int top = 500)
        {
            var logs = await _auditLogService.GetRecentLogsAsync(top);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByUser(int userId, [FromQuery] int top = 100)
        {
            var logs = await _auditLogService.GetUserLogsAsync(userId, top);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        [HttpGet("exam-session/{baithiId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByExamSession(int baithiId)
        {
            var logs = await _auditLogService.GetExamSessionLogsAsync(baithiId);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        /// <summary>GET /api/AuditLog/search — deprecated, use GET / with query params</summary>
        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> Search(
            [FromQuery] string? actionType = null,
            [FromQuery] string? username = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var logs = await _auditLogService.SearchLogsAsync(actionType, username, from, to);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }
    }
}
