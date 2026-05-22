using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Cauhoi
{
    public int Id { get; set; }

    public int? IdDanhMuc { get; set; }

    public int? IdLoaiCauHoi { get; set; }

    public string? NoiDung { get; set; }

    public double? Diem { get; set; }

    public int? NguoiTao { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public int? NguoiCapNhat { get; set; }

    public bool? DaXoa { get; set; }

    public string? DoKho { get; set; }

    public string? KhoaPhong { get; set; }

    public string? HinhAnh { get; set; }

    public virtual ICollection<Chitietlambai> Chitietlambais { get; set; } = new List<Chitietlambai>();

    public virtual ICollection<DethiCauhoi> DethiCauhois { get; set; } = new List<DethiCauhoi>();

    public virtual Danhmucauhoi? IdDanhMucNavigation { get; set; }

    public virtual Loaicauhoi? IdLoaiCauHoiNavigation { get; set; }

    public virtual ICollection<Luachon> Luachons { get; set; } = new List<Luachon>();
}
