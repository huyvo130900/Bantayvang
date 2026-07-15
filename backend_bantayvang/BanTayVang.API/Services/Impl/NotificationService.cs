using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Notification;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using BanTayVang.API.Hubs;

namespace BanTayVang.API.Services.Impl
{
    public class NotificationService : Services.Interfaces.INotificationService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            BanTayVangDbContext context,
            ILogger<NotificationService> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
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

        public async Task<BaseResponseDto<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createDto, int senderId)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                if (sender == null)
                    return new BaseResponseDto<NotificationDto> { Success = false, Message = "Không tìm thấy người gửi" };

                var isSenderAdmin = sender.RoleId == 1;
                var isSenderDeptManager = sender.RoleId == 5;

                if (!isSenderAdmin && !isSenderDeptManager)
                {
                    return new BaseResponseDto<NotificationDto> { Success = false, Message = "Bạn không có quyền gửi thông báo" };
                }

                var targetUsers = new List<User>();

                // Case 1: Send to specific user
                if (createDto.UserId.HasValue)
                {
                    var targetUser = await _context.Users.FindAsync(createDto.UserId.Value);
                    if (targetUser == null)
                        return new BaseResponseDto<NotificationDto> { Success = false, Message = "Không tìm thấy người nhận" };

                    if (isSenderDeptManager && targetUser.Department != sender.Department)
                    {
                        return new BaseResponseDto<NotificationDto> { Success = false, Message = "Bạn chỉ có thể gửi thông báo cho nhân viên thuộc khoa của mình" };
                    }

                    targetUsers.Add(targetUser);
                }
                // Case 2: Send to specific department
                else if (!string.IsNullOrEmpty(createDto.Department))
                {
                    if (isSenderDeptManager && createDto.Department != sender.Department)
                    {
                        return new BaseResponseDto<NotificationDto> { Success = false, Message = "Bạn chỉ có thể gửi thông báo cho khoa của mình" };
                    }

                    var query = _context.Users.Where(u => u.Department == createDto.Department && u.Status == true);

                    if (isSenderDeptManager)
                    {
                        // Quản lý khoa gửi -> Chỉ thí sinh trong khoa nhận (role 3), admin không nhận.
                        query = query.Where(u => u.RoleId == 3);
                    }
                    else if (isSenderAdmin)
                    {
                        // Admin gửi -> Cả quản lý khoa (role 5) và thí sinh (role 3) đều nhận
                        query = query.Where(u => u.RoleId == 3 || u.RoleId == 5);
                    }

                    targetUsers = await query.ToListAsync();
                }
                // Case 3: Broadcast to all
                else
                {
                    if (!isSenderAdmin)
                    {
                        return new BaseResponseDto<NotificationDto> { Success = false, Message = "Chỉ Admin mới có quyền gửi thông báo cho tất cả người dùng" };
                    }

                    targetUsers = await _context.Users.Where(u => u.Status == true).ToListAsync();
                }

                if (!targetUsers.Any())
                {
                    return new BaseResponseDto<NotificationDto> { Success = false, Message = "Không tìm thấy người dùng nhận thông báo phù hợp" };
                }

                var createdNotifications = new List<Notification>();
                foreach (var user in targetUsers)
                {
                    var notification = new Notification
                    {
                        UserId = user.Id,
                        Title = createDto.Title,
                        Message = createDto.Message,
                        Type = createDto.Type ?? "Info",
                        RelatedUrl = createDto.RelatedUrl,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                    createdNotifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                // Push notifications via SignalR to each user in real-time
                foreach (var notification in createdNotifications)
                {
                    if (notification.UserId.HasValue)
                    {
                        var dto = new NotificationDto
                        {
                            Id = notification.Id,
                            UserId = notification.UserId,
                            Title = notification.Title,
                            Message = notification.Message,
                            Type = notification.Type,
                            IsRead = notification.IsRead,
                            CreatedAt = notification.CreatedAt,
                            RelatedUrl = notification.RelatedUrl
                        };

                        await _hubContext.Clients.Group($"user-{notification.UserId.Value}").SendAsync("ReceiveNotification", dto);
                    }
                }

                var firstNotif = createdNotifications.First();
                return new BaseResponseDto<NotificationDto>
                {
                    Success = true,
                    Message = $"Đã gửi thông báo thành công tới {createdNotifications.Count} người dùng",
                    Data = new NotificationDto
                    {
                        Id = firstNotif.Id,
                        UserId = firstNotif.UserId,
                        Title = firstNotif.Title,
                        Message = firstNotif.Message,
                        Type = firstNotif.Type,
                        IsRead = firstNotif.IsRead,
                        CreatedAt = firstNotif.CreatedAt,
                        RelatedUrl = firstNotif.RelatedUrl
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

        public async Task<BaseResponseDto> BroadcastAsync(string title, string message, string type = "Info")
        {
            try
            {
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1) ?? new User { Id = 1 };
                var dto = new CreateNotificationDto
                {
                    Title = title,
                    Message = message,
                    Type = type
                };
                var result = await CreateNotificationAsync(dto, admin.Id);
                return new BaseResponseDto { Success = result.Success, Message = result.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting");
                return new BaseResponseDto { Success = false, Message = ex.Message };
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

        public async Task<BaseResponseDto<List<ExamScheduleDto>>> GetUpcomingExamsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var exams = await _context.ExamPapers
                    .Where(d => d.Status == "Active" && d.StartTime != null && d.StartTime > now)
                    .Include(d => d.ExamPaperQuestions)
                    .OrderBy(d => d.StartTime)
                    .Take(20)
                    .Select(d => new ExamScheduleDto
                    {
                        ExamId = d.Id,
                        ExamPaperCode = d.ExamPaperCode,
                        ExamPaperName = d.ExamPaperName,
                        StartTime = d.StartTime,
                        DurationMinutes = d.DurationMinutes,
                        EndTime = d.StartTime!.Value.AddMinutes(d.DurationMinutes ?? 60),
                        Status = d.Status,
                        TotalQuestions = d.ExamPaperQuestions.Count,
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
                var allExams = await _context.ExamPapers
                    .Where(d => d.Status == "Active" && d.StartTime != null && d.StartTime <= now)
                    .Include(d => d.ExamPaperQuestions)
                    .OrderByDescending(d => d.StartTime)
                    .Take(50)
                    .ToListAsync();

                var result = allExams.Select(d =>
                {
                    var endTime = d.StartTime!.Value.AddMinutes(d.DurationMinutes ?? 60);
                    var isAvailable = now <= endTime;
                    return new ExamScheduleDto
                    {
                        ExamId = d.Id,
                        ExamPaperCode = d.ExamPaperCode,
                        ExamPaperName = d.ExamPaperName,
                        StartTime = d.StartTime,
                        DurationMinutes = d.DurationMinutes,
                        EndTime = endTime,
                        Status = d.Status,
                        TotalQuestions = d.ExamPaperQuestions.Count,
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