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

        public int? KhoaPhongId { get; set; }

        [ForeignKey("KhoaPhongId")]
        public virtual KhoaPhong? KhoaPhong { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = "DangChuanBi"; // DangChuanBi, DangDienRa, TamDung, DaKetThuc

        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }

        public int? NguoiTao { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime? NgayCapNhat { get; set; }

        [StringLength(100)]
        public string? DonViToChuc { get; set; }

        public int? SoCauDungToiThieu { get; set; }

        public int? TongSoCauHoi { get; set; }

        public int? ThoiGianLamBai { get; set; }
    }
}