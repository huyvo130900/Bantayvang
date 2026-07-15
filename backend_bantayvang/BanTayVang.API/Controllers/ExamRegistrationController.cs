using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.ExamRegistration;
using BanTayVang.API.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Linq;
using BanTayVang.API.Configuration;

namespace BanTayVang.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamRegistrationController : ControllerBase
    {
        private readonly IExamRegistrationService _examRegistrationService;

        public ExamRegistrationController(IExamRegistrationService dangKyThiService)
        {
            _examRegistrationService = dangKyThiService;
        }

        [HttpPost("public")]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateExamRegistrationDto dto)
        {
            try
            {
                var result = await _examRegistrationService.CreateAsync(dto);
                return Ok(new { Message = "Đăng ký thành công. Vui lòng chờ xét duyệt.", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> GetPending()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int? khoaPhongId = null;

            if (userRole == "DeptManager")
            {
                var khoaIdClaim = User.FindFirst("id_khoa_quan_ly")?.Value;
                if (int.TryParse(khoaIdClaim, out int kId))
                {
                    khoaPhongId = kId;
                }
            }

            var list = await _examRegistrationService.GetPendingAsync(khoaPhongId);
            return Ok(list);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Approve(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int? khoaPhongId = null;
            if (userRole == "DeptManager")
            {
                var khoaIdClaim = User.FindFirst("id_khoa_quan_ly")?.Value;
                if (int.TryParse(khoaIdClaim, out int kId)) khoaPhongId = kId;
            }

            var reg = await _examRegistrationService.GetPendingAsync(khoaPhongId);
            if (!reg.Any(r => r.Id == id)) return BadRequest(new { Message = "Bạn không có quyền hoặc đơn không tồn tại." });

            var success = await _examRegistrationService.ApproveAsync(id, userId);
            if (success)
                return Ok(new { Message = "Phê duyệt thành công. Tài khoản đã được tạo." });
            return BadRequest(new { Message = "Không thể phê duyệt đơn này." });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int? khoaPhongId = null;
            if (userRole == "DeptManager")
            {
                var khoaIdClaim = User.FindFirst("id_khoa_quan_ly")?.Value;
                if (int.TryParse(khoaIdClaim, out int kId)) khoaPhongId = kId;
            }

            var reg = await _examRegistrationService.GetPendingAsync(khoaPhongId);
            if (!reg.Any(r => r.Id == id)) return BadRequest(new { Message = "Bạn không có quyền hoặc đơn không tồn tại." });

            var success = await _examRegistrationService.RejectAsync(id, dto.Reason, userId);
            if (success)
                return Ok(new { Message = "Đã từ chối đơn đăng ký." });
            return BadRequest(new { Message = "Không thể từ chối đơn này." });
        }
    }

    public class RejectDto
    {
        public string? Reason { get; set; }
    }
}
