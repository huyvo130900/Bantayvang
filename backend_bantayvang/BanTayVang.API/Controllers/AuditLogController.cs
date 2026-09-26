using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManagementOnly")]
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
            // DeptManager: chỉ thấy log của khoa mình
            // BUG FIX: every SearchLogsAsync/GetRecentLogsAsync/etc. filter below only applies a
            // department restriction when the string is non-empty (`if (!string.IsNullOrEmpty(department))`)
            // - so a DeptManager whose Department claim came back empty (confirmed reachable: bulk
            // Excel user-import doesn't require a department for management-role rows, unlike the
            // single-user create/edit form) would silently see EVERY department's logs, including
            // Admin's own actions, instead of none. Fail closed instead of fail open.
            string? myDepartment = null;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return Forbid();
                }
            }

            var logs = await _auditLogService.SearchLogsAsync(actionType, username, from, to, page, pageSize, myDepartment);
            var total = await _auditLogService.GetTotalCountAsync(actionType, username, from, to, myDepartment);

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

        /// <summary>GET /api/AuditLog/recent — nhanh, top N. DeptManager: chỉ thấy log của khoa mình.</summary>
        [HttpGet("recent")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetRecent([FromQuery] int top = 500)
        {
            // BUG FIX: fail closed (Forbid) instead of silently showing all-department logs when
            // a DeptManager's Department claim is empty - see matching comment in GetLogs above.
            if (DepartmentAuthHelper.IsDeptManager(User) && string.IsNullOrEmpty(DepartmentAuthHelper.GetDepartmentClaim(User)))
            {
                return Forbid();
            }
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;
            var logs = await _auditLogService.GetRecentLogsAsync(top, myDepartment);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByUser(int userId, [FromQuery] int top = 100)
        {
            // BUG FIX: fail closed (Forbid) instead of silently showing all-department logs when
            // a DeptManager's Department claim is empty - see matching comment in GetLogs above.
            if (DepartmentAuthHelper.IsDeptManager(User) && string.IsNullOrEmpty(DepartmentAuthHelper.GetDepartmentClaim(User)))
            {
                return Forbid();
            }
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;
            var logs = await _auditLogService.GetUserLogsAsync(userId, top, myDepartment);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        [HttpGet("exam-session/{examSubmissionId}")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> GetByExamSession(int examSubmissionId)
        {
            // BUG FIX: fail closed (Forbid) instead of silently showing all-department logs when
            // a DeptManager's Department claim is empty - see matching comment in GetLogs above.
            if (DepartmentAuthHelper.IsDeptManager(User) && string.IsNullOrEmpty(DepartmentAuthHelper.GetDepartmentClaim(User)))
            {
                return Forbid();
            }
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;
            var logs = await _auditLogService.GetExamSessionLogsAsync(examSubmissionId, myDepartment);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }

        /// <summary>GET /api/AuditLog/search — deprecated, use GET / with query params. DeptManager: chỉ thấy log của khoa mình.</summary>
        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<List<AuditLogEntry>>>> Search(
            [FromQuery] string? actionType = null,
            [FromQuery] string? username = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            // BUG FIX: fail closed (Forbid) instead of silently showing all-department logs when
            // a DeptManager's Department claim is empty - see matching comment in GetLogs above.
            if (DepartmentAuthHelper.IsDeptManager(User) && string.IsNullOrEmpty(DepartmentAuthHelper.GetDepartmentClaim(User)))
            {
                return Forbid();
            }
            var myDepartment = DepartmentAuthHelper.IsDeptManager(User) ? DepartmentAuthHelper.GetDepartmentClaim(User) : null;
            var logs = await _auditLogService.SearchLogsAsync(actionType, username, from, to, department: myDepartment);
            return Ok(new BaseResponseDto<List<AuditLogEntry>> { Success = true, Data = logs });
        }
    }
}
