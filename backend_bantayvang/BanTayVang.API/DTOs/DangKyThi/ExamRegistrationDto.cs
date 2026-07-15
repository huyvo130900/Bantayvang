using System;

namespace BanTayVang.API.DTOs.ExamRegistration
{
    public class ExamRegistrationDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Cccd { get; set; } = null!;
        public string SoDienThoai { get; set; } = null!;
        public string? Email { get; set; }
        public string? DonViCongTac { get; set; }
        public string? ChuyenNganh { get; set; }
        public int? DepartmentId { get; set; }
        public string? TenKhoaPhong { get; set; }
        public string? MucDichThi { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime NgayDangKy { get; set; }
        public string? GhiChu { get; set; }
    }
}
