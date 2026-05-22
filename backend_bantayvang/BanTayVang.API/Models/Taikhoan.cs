using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Taikhoan
{
    public int Id { get; set; }

    public string? MaNhanVien { get; set; }

    public string? TenDangNhap { get; set; }

    public string? MatKhau { get; set; }

    public string? ChucDanh { get; set; }

    public string? KhoaPhong { get; set; }

    // JWT Authentication fields added by migration
    public string? Email { get; set; }

    public string? HoTen { get; set; }

    public int? IdVaiTro { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? LanDangNhapCuoi { get; set; }

    public virtual ICollection<Baithi> Baithis { get; set; } = new List<Baithi>();

    public virtual ICollection<Phiendangnhap> Phiendangnhaps { get; set; } = new List<Phiendangnhap>();

    public virtual ICollection<TaikhoanVaitro> TaikhoanVaitros { get; set; } = new List<TaikhoanVaitro>();
}
