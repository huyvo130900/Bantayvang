using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

[Table("DANG_KY_THI")]
public partial class DangKyThi
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string HoTen { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Cccd { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string SoDienThoai { get; set; } = null!;

    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string MatKhauHash { get; set; } = null!;

    [MaxLength(255)]
    public string? DonViCongTac { get; set; }

    [MaxLength(255)]
    public string? ChuyenNganh { get; set; }

    public int? KhoaPhongId { get; set; }

    [MaxLength(255)]
    public string? MucDichThi { get; set; }

    [MaxLength(50)]
    public string TrangThai { get; set; } = "Pending"; // Pending, Approved, Rejected

    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [MaxLength(1000)]
    public string? GhiChu { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime? NgayDuyet { get; set; }

    [ForeignKey("KhoaPhongId")]
    public virtual KhoaPhong? KhoaPhong { get; set; }
}
