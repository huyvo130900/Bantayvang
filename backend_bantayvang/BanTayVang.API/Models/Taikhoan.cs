using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class Taikhoan
{
    public int Id { get; set; }

    public string? MaNhanVien { get; set; }

    public string? TenDangNhap { get; set; }

    public string? MatKhau { get; set; }

    public string? ChucDanh { get; set; }

    public string? KhoaPhong { get; set; }

    public string? HoTen { get; set; }

    public int? IdVaiTro { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? LanDangNhapCuoi { get; set; }

    /// <summary>
    /// FK -> KHOA_PHONG.Id - chỉ dùng cho role DeptManager (ID=5)
    /// </summary>
    public int? IdKhoaQuanLy { get; set; }

    [ForeignKey("IdKhoaQuanLy")]
    public virtual KhoaPhong? KhoaQuanLy { get; set; }

    public virtual ICollection<Baithi> Baithis { get; set; } = new List<Baithi>();

    public virtual ICollection<Phiendangnhap> Phiendangnhaps { get; set; } = new List<Phiendangnhap>();

    public virtual ICollection<TaikhoanVaitro> TaikhoanVaitros { get; set; } = new List<TaikhoanVaitro>();
}
