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
        public int? UserId { get; set; } // null = broadcast/dept
        public string? Department { get; set; } // specific department name

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
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public DateTime? StartTime { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailabilityMessage { get; set; } = string.Empty;
    }
}