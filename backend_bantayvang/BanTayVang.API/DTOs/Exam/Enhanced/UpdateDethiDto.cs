using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    /// <summary>
    /// DTO for updating exam with security validation
    /// Follows OWASP input validation standards
    /// </summary>
    public class UpdateDethiDto
    {
        [Required(ErrorMessage = "Exam ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Exam code is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Exam code must be 3-50 characters")]
        [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "Exam code can only contain uppercase letters, numbers, underscore and dash")]
        public string MaDeThi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam name is required")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Exam name must be 5-255 characters")]
        // OWASP: Prevent XSS
        [RegularExpression(@"^[^<>""'%;()&+]*$", ErrorMessage = "Exam name contains invalid characters")]
        public string TenDeThi { get; set; } = string.Empty;

        [Range(1, 300, ErrorMessage = "Exam duration must be between 1 and 300 minutes")]
        public int ThoiGianLamBai { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public DateTime ThoiGianBatDau { get; set; }

        [RegularExpression("^(Draft|Active|Inactive|Archived)$", ErrorMessage = "Invalid status")]
        public string TrangThai { get; set; } = "Draft";

        // OWASP: Validate question IDs to prevent injection
        public List<int> DanhSachIdCauHoi { get; set; } = new();

        // Audit fields
        public int NguoiCapNhat { get; set; }
        public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
        public string? LyDoCapNhat { get; set; }

        /// <summary>
        /// Custom validation for business rules
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // OWASP: Validate start time is not in the past (with tolerance)
            if (ThoiGianBatDau < DateTime.UtcNow.AddMinutes(-5))
            {
                results.Add(new ValidationResult(
                    "Start time cannot be in the past",
                    new[] { nameof(ThoiGianBatDau) }));
            }

            // OWASP: Validate reasonable number of questions
            if (DanhSachIdCauHoi.Count > 200)
            {
                results.Add(new ValidationResult(
                    "Too many questions (max 200)",
                    new[] { nameof(DanhSachIdCauHoi) }));
            }

            // OWASP: Validate question IDs are positive
            if (DanhSachIdCauHoi.Any(id => id <= 0))
            {
                results.Add(new ValidationResult(
                    "Invalid question IDs",
                    new[] { nameof(DanhSachIdCauHoi) }));
            }

            return results;
        }
    }
}