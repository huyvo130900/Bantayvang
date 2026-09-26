using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Notification;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Notification & Schedule controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Lấy thông báo của user hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<NotificationDto>>>> GetMyNotifications([FromQuery] bool? unreadOnly = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.GetUserNotificationsAsync(userId.Value, unreadOnly);
            return Ok(result);
        }

        /// <summary>
        /// Số thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<BaseResponseDto<int>>> GetUnreadCount()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Tạo thông báo cho user
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<NotificationDto>>> Create([FromBody] CreateNotificationDto dto)
        {
            var senderId = GetUserId();
            if (senderId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.CreateNotificationAsync(dto, senderId.Value);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu 1 thông báo đã đọc
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<ActionResult<BaseResponseDto>> MarkAsRead(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<ActionResult<BaseResponseDto>> MarkAllAsRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseDto>> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _notificationService.DeleteNotificationAsync(id, userId.Value);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Broadcast thông báo cho tất cả users
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> Broadcast([FromBody] CreateNotificationDto dto)
        {
            var senderId = GetUserId();
            if (senderId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            dto.UserId = null;
            dto.Department = null;
            var result = await _notificationService.CreateNotificationAsync(dto, senderId.Value);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lịch các đề thi sắp diễn ra
        /// </summary>
        [HttpGet("upcoming-exams")]
        public async Task<ActionResult<BaseResponseDto<List<ExamScheduleDto>>>> GetUpcomingExams()
        {
            var result = await _notificationService.GetUpcomingExamsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lịch các đề thi đang/đã diễn ra
        /// </summary>
        [HttpGet("current-exams")]
        public async Task<ActionResult<BaseResponseDto<List<ExamScheduleDto>>>> GetCurrentExams()
        {
            var result = await _notificationService.GetCurrentExamsAsync();
            return Ok(result);
        }

        private int? GetUserId() => HttpContext.Items["UserId"] as int?;
    }
}