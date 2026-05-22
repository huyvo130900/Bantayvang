using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Notification;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class NotificationService : Services.Interfaces.INotificationService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(BanTayVangDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<List<NotificationDto>>> GetUserNotificationsAsync(int userId, bool? unreadOnly = null)
        {
            try
            {
                var query = _context.Notifications.AsQueryable()
                    .Where(n => n.UserId == userId || n.UserId == null);

                if (unreadOnly == true)
                    query = query.Where(n => !n.IsRead);

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        RelatedUrl = n.RelatedUrl
                    })
                    .ToListAsync();

                return new BaseResponseDto<List<NotificationDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = notifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return new BaseResponseDto<List<NotificationDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông báo",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<int>> GetUnreadCountAsync(int userId)
        {
            try
            {
                var count = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                    .CountAsync();

                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return new BaseResponseDto<int>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = 0
                };
            }
        }

        public async Task<BaseResponseDto<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createDto)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = createDto.UserId,
                    Title = createDto.Title,
                    Message = createDto.Message,
                    Type = createDto.Type,
                    RelatedUrl = createDto.RelatedUrl,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return new BaseResponseDto<NotificationDto>
                {
                    Success = true,
                    Message = "Tạo thông báo thành công",
                    Data = new NotificationDto
                    {
                        Id = notification.Id,
                        UserId = notification.UserId,
                        Title = notification.Title,
                        Message = notification.Message,
                        Type = notification.Type,
                        IsRead = notification.IsRead,
                        CreatedAt = notification.CreatedAt,
                        RelatedUrl = notification.RelatedUrl
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return new BaseResponseDto<NotificationDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo thông báo",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && (n.UserId == userId || n.UserId == null));

                if (notification == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy thông báo" };

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã đánh dấu đã đọc" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                    .ToListAsync();

                foreach (var n in notifications)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = $"Đã đánh dấu {notifications.Count} thông báo" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy thông báo" };

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã xóa thông báo" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> BroadcastAsync(string title, string message, string type = "Info")
        {
            try
            {
                var notification = new Notification
                {
                    UserId = null, // broadcast
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã gửi thông báo broadcast" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<List<ExamScheduleDto>>> GetUpcomingExamsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var exams = await _context.Dethis
                    .Where(d => d.TrangThai == "Active" && d.ThoiGianBatDau != null && d.ThoiGianBatDau > now)
                    .Include(d => d.DethiCauhois)
                    .OrderBy(d => d.ThoiGianBatDau)
                    .Take(20)
                    .Select(d => new ExamScheduleDto
                    {
                        ExamId = d.Id,
                        MaDeThi = d.MaDeThi,
                        TenDeThi = d.TenDeThi,
                        ThoiGianBatDau = d.ThoiGianBatDau,
                        ThoiGianLamBai = d.ThoiGianLamBai,
                        ThoiGianKetThuc = d.ThoiGianBatDau!.Value.AddMinutes(d.ThoiGianLamBai ?? 60),
                        TrangThai = d.TrangThai,
                        SoCauHoi = d.DethiCauhois.Count,
                        IsAvailable = false,
                        AvailabilityMessage = "Chưa đến giờ thi"
                    })
                    .ToListAsync();

                return new BaseResponseDto<List<ExamScheduleDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = exams
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming exams");
                return new BaseResponseDto<List<ExamScheduleDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamScheduleDto>()
                };
            }
        }

        public async Task<BaseResponseDto<List<ExamScheduleDto>>> GetCurrentExamsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var allExams = await _context.Dethis
                    .Where(d => d.TrangThai == "Active" && d.ThoiGianBatDau != null && d.ThoiGianBatDau <= now)
                    .Include(d => d.DethiCauhois)
                    .OrderByDescending(d => d.ThoiGianBatDau)
                    .Take(50)
                    .ToListAsync();

                var result = allExams.Select(d =>
                {
                    var endTime = d.ThoiGianBatDau!.Value.AddMinutes(d.ThoiGianLamBai ?? 60);
                    var isAvailable = now <= endTime;
                    return new ExamScheduleDto
                    {
                        ExamId = d.Id,
                        MaDeThi = d.MaDeThi,
                        TenDeThi = d.TenDeThi,
                        ThoiGianBatDau = d.ThoiGianBatDau,
                        ThoiGianLamBai = d.ThoiGianLamBai,
                        ThoiGianKetThuc = endTime,
                        TrangThai = d.TrangThai,
                        SoCauHoi = d.DethiCauhois.Count,
                        IsAvailable = isAvailable,
                        AvailabilityMessage = isAvailable ? "Đang diễn ra" : "Đã kết thúc"
                    };
                }).ToList();

                return new BaseResponseDto<List<ExamScheduleDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current exams");
                return new BaseResponseDto<List<ExamScheduleDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamScheduleDto>()
                };
            }
        }
    }
}