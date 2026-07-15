using System;

namespace BanTayVang.API.DTOs.DangKyThi
{
    public class DangKyThiDto
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = null!;
        public string Cccd { get; set; } = null!;
        public string SoDienThoai { get; set; } = null!;
        public string? Email { get; set; }
        public string? DonViCongTac { get; set; }
        public string? ChuyenNganh { get; set; }
        public int? KhoaPhongId { get; set; }
        public string? TenKhoaPhong { get; set; }
        public string? MucDichThi { get; set; }
        public string TrangThai { get; set; } = "Pending";
        public DateTime NgayDangKy { get; set; }
        public string? GhiChu { get; set; }
    }
}
