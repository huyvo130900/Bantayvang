using System;

namespace BanTayVang.API.DTOs.ExamRegistration
{
    public class ExamRegistrationDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string IdCardNumber { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }
        public string? WorkUnit { get; set; }
        public string? Major { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ExamPurpose { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RegistrationDate { get; set; }
        public string? Notes { get; set; }
    }
}
