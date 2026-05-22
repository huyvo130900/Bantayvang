using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.KyThi
{
    public class KyThiDto
    {
        public int Id { get; set; }
        public string? MaKyThi { get; set; }
        public string? TenKyThi { get; set; }
        public string? MoTa { get; set; }
        public string? LoaiKyThi { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? DonViToChuc { get; set; }
        public DateTime NgayTao { get; set; }
        public int SoCaThi { get; set; }
        public int TongThiSinh { get; set; }
        public List<CaThiDto> DanhSachCaThi { get; set; } = new();
    }

    public class CreateKyThiDto
    {
        [Required]
        [StringLength(50)]
        public string MaKyThi { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TenKyThi { get; set; } = string.Empty;

        public string? MoTa { get; set; }
        public string? LoaiKyThi { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? DonViToChuc { get; set; }
    }

    public class UpdateKyThiDto : CreateKyThiDto
    {
        public string TrangThai { get; set; } = "DangChuanBi";
    }

    public class CaThiDto
    {
        public int Id { get; set; }
        public int KyThiId { get; set; }
        public int? DeThiId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenCa { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public int? SoLuongToiDa { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public int SoThiSinhDaDangKy { get; set; }
    }

    public class CreateCaThiDto
    {
        [Required]
        public int KyThiId { get; set; }

        public int? DeThiId { get; set; }

        [Required]
        [StringLength(100)]
        public string TenCa { get; set; } = string.Empty;

        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public int? SoLuongToiDa { get; set; }
        public string? GhiChu { get; set; }
    }
}