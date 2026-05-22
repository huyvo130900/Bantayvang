using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Notification;

namespace BanTayVang.API.Services.Interfaces
{
    public interface INotificationService
    {
        Task<BaseResponseDto<List<NotificationDto>>> GetUserNotificationsAsync(int userId, bool? unreadOnly = null);
        Task<BaseResponseDto<int>> GetUnreadCountAsync(int userId);
        Task<BaseResponseDto<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createDto);
        Task<BaseResponseDto> MarkAsReadAsync(int notificationId, int userId);
        Task<BaseResponseDto> MarkAllAsReadAsync(int userId);
        Task<BaseResponseDto> DeleteNotificationAsync(int notificationId, int userId);
        Task<BaseResponseDto> BroadcastAsync(string title, string message, string type = "Info");

        // Schedule
        Task<BaseResponseDto<List<ExamScheduleDto>>> GetUpcomingExamsAsync();
        Task<BaseResponseDto<List<ExamScheduleDto>>> GetCurrentExamsAsync();
    }
}