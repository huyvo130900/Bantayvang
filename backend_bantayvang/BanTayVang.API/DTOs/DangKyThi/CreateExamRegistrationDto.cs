using System;
using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.ExamRegistration
{
    public class CreateExamRegistrationDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số CCCD là bắt buộc")]
        public string Cccd { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string PhoneNumber { get; set; } = null!;

        public string? Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = null!;

        public string? WorkUnit { get; set; }

        public string? ChuyenNganh { get; set; }

        public int? DepartmentId { get; set; }

        public string? MucDichThi { get; set; }
    }
}
