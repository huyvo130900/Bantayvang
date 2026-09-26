using System;
using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.ExamRegistration
{
    public class CreateExamRegistrationDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số CCCD là bắt buộc")]
        public string IdCardNumber { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        // BUG FIX: had no length/strength constraint at all - confirmed live a 1-character password
        // ("a") was accepted and, once an admin approves the registration, becomes a real login-
        // capable account. StringLength here is just the DTO-level floor (matches RegisterDto's
        // convention); the real complexity check runs server-side via ValidatePasswordStrength in
        // ExamRegistrationService.CreateAsync.
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        public string Password { get; set; } = null!;

        public string? WorkUnit { get; set; }

        public string? Major { get; set; }

        // BUG FIX: was optional - a registration approved with DepartmentId == null produces a
        // User with Department == null (see ExamRegistrationService.ApproveAsync), and
        // ExamCampaignService.GetAllAsync treats an empty Department as "no filter", so that
        // account could see every exam campaign of every department instead of just its own.
        [Required(ErrorMessage = "Vui lòng chọn khoa/phòng ban")]
        public int? DepartmentId { get; set; }

        public string? ExamPurpose { get; set; }
    }
}
