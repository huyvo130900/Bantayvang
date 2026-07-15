using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    /// <summary>
    /// DTO for updating exam with security validation
    /// Follows OWASP input validation standards
    /// </summary>
    public class UpdateExamPaperDto
    {
        [Required(ErrorMessage = "Exam ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Exam code is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Exam code must be 3-50 characters")]
        [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "Exam code can only contain uppercase letters, numbers, underscore and dash")]
        public string ExamPaperCode { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 5, ErrorMessage = "Exam name must be 5-255 characters")]
        // OWASP: Prevent XSS
        [RegularExpression(@"^[^<>""'%;()&+]*$", ErrorMessage = "Exam name contains invalid characters")]
        public string? ExamPaperName { get; set; }

        [Range(1, 1008000, ErrorMessage = "Exam duration must be between 1 and 1008000 minutes")]
        public int? DurationMinutes { get; set; }

        public DateTime? StartTime { get; set; }

        [RegularExpression("^(Draft|Active|Inactive|Archived|Published)$", ErrorMessage = "Invalid status")]
        public string Status { get; set; } = "Draft";

        // OWASP: Validate question IDs to prevent injection
        public List<int> QuestionIds { get; set; } = new();

        // Audit fields
        public int UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? LyDoCapNhat { get; set; }

        /// <summary>
        /// Kỳ thi liên kết
        /// </summary>
        public int? ExamCampaignId { get; set; }

        /// <summary>
        /// Custom validation for business rules
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // OWASP: Validate start time is not in the past (with tolerance)
            if (StartTime.HasValue && StartTime.Value < DateTime.UtcNow.AddMinutes(-5))
            {
                results.Add(new ValidationResult(
                    "Start time cannot be in the past",
                    new[] { nameof(StartTime) }));
            }

            // OWASP: Validate reasonable number of questions
            if (QuestionIds.Count > 200)
            {
                results.Add(new ValidationResult(
                    "Too many questions (max 200)",
                    new[] { nameof(QuestionIds) }));
            }

            // OWASP: Validate question IDs are positive
            if (QuestionIds.Any(id => id <= 0))
            {
                results.Add(new ValidationResult(
                    "Invalid question IDs",
                    new[] { nameof(QuestionIds) }));
            }

            return results;
        }
    }
}