using System;
using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.DangKyThi
{
    public class CreateDangKyThiDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Số CCCD là bắt buộc")]
        public string Cccd { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string SoDienThoai { get; set; } = null!;

        public string? Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string MatKhau { get; set; } = null!;

        public string? DonViCongTac { get; set; }

        public string? ChuyenNganh { get; set; }

        public int? KhoaPhongId { get; set; }

        public string? MucDichThi { get; set; }
    }
}
