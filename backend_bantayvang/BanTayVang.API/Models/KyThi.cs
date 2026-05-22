using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Kỳ thi - chứa nhiều đề thi và ca thi
    /// Ví dụ: "Kỳ thi Bàn tay vàng Q2/2026", "Kiểm soát nhiễm khuẩn 2026"
    /// </summary>
    [Table("KyThi")]
    public class KyThi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MaKyThi { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TenKyThi { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MoTa { get; set; }

        [StringLength(100)]
        public string? LoaiKyThi { get; set; } // BanTayVang, KiemSoatNhiemKhuan, AnToanNguoiBenh, CNTT

        [StringLength(50)]
        public string TrangThai { get; set; } = "DangChuanBi"; // DangChuanBi, DangDienRa, TamDung, DaKetThuc

        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }

        public int? NguoiTao { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime? NgayCapNhat { get; set; }

        [StringLength(100)]
        public string? DonViToChuc { get; set; }

        public virtual ICollection<CaThi> CaThis { get; set; } = new List<CaThi>();
    }

    /// <summary>
    /// Ca thi - chia kỳ thi thành nhiều ca
    /// </summary>
    [Table("CaThi")]
    public class CaThi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KyThiId { get; set; }

        public int? DeThiId { get; set; }

        [Required]
        [StringLength(100)]
        public string TenCa { get; set; } = string.Empty;

        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }

        public int? SoLuongToiDa { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = "ChuaBatDau"; // ChuaBatDau, DangDienRa, DaKetThuc

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [ForeignKey("KyThiId")]
        public virtual KyThi? KyThi { get; set; }

        [ForeignKey("DeThiId")]
        public virtual Dethi? DeThi { get; set; }
    }
}