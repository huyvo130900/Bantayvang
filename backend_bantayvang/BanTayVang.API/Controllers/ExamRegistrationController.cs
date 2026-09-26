using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.ExamRegistration;
using BanTayVang.API.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Linq;
using BanTayVang.API.Configuration;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Quản lý đơn đăng ký thi của Thí sinh ngoại.
    /// Phân quyền:
    ///   - Admin: xem và duyệt tất cả đơn của mọi khoa.
    ///   - DeptManager: chỉ xem và duyệt đơn thuộc đúng khoa mình quản lý.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ExamRegistrationController : ControllerBase
    {
        private readonly IExamRegistrationService _examRegistrationService;
        private readonly BanTayVangDbContext _context;

        public ExamRegistrationController(
            IExamRegistrationService examRegistrationService,
            BanTayVangDbContext context)
        {
            _examRegistrationService = examRegistrationService;
            _context = context;
        }

        // -------------------------------------------------------
        // POST /api/ExamRegistration/public
        // Thí sinh ngoại tự nộp đơn (không cần đăng nhập)
        // -------------------------------------------------------
        [HttpPost("public")]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateExamRegistrationDto dto)
        {
            try
            {
                await _examRegistrationService.CreateAsync(dto);
                // BUG FIX (PII disclosure): CreateAsync returns null when the CCCD already has an
                // account or a pending application, instead of throwing a distinct message for
                // each case, specifically so an anonymous caller cannot tell a real new registration
                // apart from probing someone else's CCCD. The message text alone isn't enough -
                // confirmed live that echoing the created row back as `Data` (populated on a real
                // new registration, null on a duplicate) let the SAME probe be answered just by
                // checking `data === null` instead of parsing the message. Never echo the result
                // back to this anonymous endpoint - the caller already has everything they submitted.
                return Ok(new { Message = "Đăng ký thành công. Vui lòng chờ xét duyệt.", Data = (object?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // -------------------------------------------------------
        // GET /api/ExamRegistration/pending
        // Admin → thấy tất cả đơn đang chờ
        // DeptManager → chỉ thấy đơn của khoa mình
        // -------------------------------------------------------
        [HttpGet("pending")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> GetPending()
        {
            int? restrictToDeptId = GetManagedDepartmentId();
            var list = await _examRegistrationService.GetPendingRegistrationsAsync(restrictToDeptId);
            return Ok(list);
        }

        // -------------------------------------------------------
        // POST /api/ExamRegistration/{id}/approve
        // Admin → duyệt bất kỳ đơn nào
        // DeptManager → chỉ duyệt đơn thuộc khoa mình
        // -------------------------------------------------------
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Approve(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            // Kiểm tra đơn tồn tại
            var reg = await _context.ExamRegistrations.FindAsync(id);
            if (reg == null || reg.Status != "Pending")
                return NotFound(new { Message = "Không tìm thấy đơn đăng ký đang chờ duyệt." });

            // Kiểm tra quyền: DeptManager chỉ được duyệt đơn của khoa mình.
            // BUG FIX: `if (myDeptId.HasValue && ...)` bỏ qua hẳn việc kiểm tra khi tài khoản Quản
            // lý khoa không có managed_department_id (vd tạo qua API tạo user đơn lẻ, đường này
            // không bắt buộc chọn khoa như luồng import hàng loạt) - cho phép duyệt bất kỳ đơn nào
            // của bất kỳ khoa nào. Phải chặn (fail-closed) khi Quản lý khoa không có khoa được gán,
            // giống cách ExamCampaignController đã làm đúng.
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "DeptManager")
            {
                int? myDeptId = GetManagedDepartmentId();
                if (myDeptId == null || reg.DepartmentId != myDeptId.Value)
                    return Forbid();
            }

            var success = await _examRegistrationService.ApproveAsync(id, userId);
            if (success)
                return Ok(new { Message = "Phê duyệt thành công. Tài khoản đã được tạo." });

            return BadRequest(new { Message = "Không thể phê duyệt đơn này." });
        }

        // -------------------------------------------------------
        // POST /api/ExamRegistration/{id}/reject
        // Admin → từ chối bất kỳ đơn nào
        // DeptManager → chỉ từ chối đơn thuộc khoa mình
        // -------------------------------------------------------
        [HttpPost("{id}/reject")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            // Kiểm tra đơn tồn tại
            var reg = await _context.ExamRegistrations.FindAsync(id);
            if (reg == null || reg.Status != "Pending")
                return NotFound(new { Message = "Không tìm thấy đơn đăng ký đang chờ duyệt." });

            // Kiểm tra quyền: DeptManager chỉ được từ chối đơn của khoa mình - xem ghi chú BUG FIX
            // trong Approve() ở trên, cùng lỗi fail-open.
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "DeptManager")
            {
                int? myDeptId = GetManagedDepartmentId();
                if (myDeptId == null || reg.DepartmentId != myDeptId.Value)
                    return Forbid();
            }

            var success = await _examRegistrationService.RejectAsync(id, dto.Reason, userId);
            if (success)
                return Ok(new { Message = "Đã từ chối đơn đăng ký." });

            return BadRequest(new { Message = "Không thể từ chối đơn này." });
        }

        // -------------------------------------------------------
        // Helper: Lấy DepartmentId mà người dùng hiện tại quản lý.
        // Trả về null nếu là Admin (Admin không bị giới hạn khoa).
        // -------------------------------------------------------
        private int? GetManagedDepartmentId()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "DeptManager") return null; // Admin → không lọc

            var claim = User.FindFirst("managed_department_id")?.Value;
            return int.TryParse(claim, out int deptId) ? deptId : null;
        }
    }

    public class RejectDto
    {
        public string? Reason { get; set; }
    }
}
