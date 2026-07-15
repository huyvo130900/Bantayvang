using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.KyThi
{
    public class KyThiDto
    {
        public int Id { get; set; }
        public string? MaKyThi { get; set; }
        public string? TenKyThi { get; set; }
        public string? MoTa { get; set; }
        public int? KhoaPhongId { get; set; }
        public string? TenKhoa { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? DonViToChuc { get; set; }
        public DateTime NgayTao { get; set; }
        public int SoLuongDeThi { get; set; }
        public int TongThiSinh { get; set; }
        public List<string> DanhSachMaDeThi { get; set; } = new();
        public string? MaDeThi { get; set; }
        public int? SoCauDungToiThieu { get; set; }
        public int? TongSoCauHoi { get; set; }
        public int? ThoiGianLamBai { get; set; }
    }

    public class CreateKyThiDto
    {
        [Required(ErrorMessage = "Mã kỳ thi là bắt buộc")]
        [StringLength(50)]
        public string MaKyThi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kỳ thi là bắt buộc")]
        [StringLength(255)]
        public string TenKyThi { get; set; } = string.Empty;

        public string? MoTa { get; set; }
        public int? KhoaPhongId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime? ThoiGianBatDau { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime? ThoiGianKetThuc { get; set; }

        public string? DonViToChuc { get; set; }
        public int? SoCauDungToiThieu { get; set; }
        public int? TongSoCauHoi { get; set; }
        public int? ThoiGianLamBai { get; set; }
    }

    public class UpdateKyThiDto : CreateKyThiDto
    {
        public string TrangThai { get; set; } = "DangChuanBi";
    }
}