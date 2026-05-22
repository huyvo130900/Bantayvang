using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Notification
{
    /// <summary>
    /// Notification DTO
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedUrl { get; set; }
    }

    /// <summary>
    /// Create notification DTO
    /// </summary>
    public class CreateNotificationDto
    {
        public int? UserId { get; set; } // null = broadcast to all

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = "Info";
        public string? RelatedUrl { get; set; }
    }

    /// <summary>
    /// Exam schedule DTO
    /// </summary>
    public class ExamScheduleDto
    {
        public int ExamId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public int? ThoiGianLamBai { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? TrangThai { get; set; }
        public int SoCauHoi { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailabilityMessage { get; set; } = string.Empty;
    }
}