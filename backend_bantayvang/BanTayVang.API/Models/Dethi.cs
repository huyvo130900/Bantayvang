using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Dethi
{
    public int Id { get; set; }

    public string? MaDeThi { get; set; }

    public string? TenDeThi { get; set; }

    public int? ThoiGianLamBai { get; set; }

    public double? TongDiem { get; set; }

    public DateTime? ThoiGianBatDau { get; set; }

    public string? LinkTruyCap { get; set; }

    public string? TrangThai { get; set; }

    public int? NguoiTao { get; set; }

    public DateTime? NgayTao { get; set; }

    // Additional properties for enhanced exam management
    public string? ChecksumData { get; set; }
    public int? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<Baithi> Baithis { get; set; } = new List<Baithi>();

    public virtual ICollection<DethiCauhoi> DethiCauhois { get; set; } = new List<DethiCauhoi>();
}
