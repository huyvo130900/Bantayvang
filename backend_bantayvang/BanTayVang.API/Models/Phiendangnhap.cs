using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Phiendangnhap
{
    public int Id { get; set; }

    public int? IdTaiKhoan { get; set; }

    public string? Token { get; set; }

    public DateTime? ThoiGianTao { get; set; }

    public DateTime? ThoiGianHetHan { get; set; }

    public string? Ip { get; set; }

    public string? ThietBiUserAgent { get; set; }

    public virtual Taikhoan? IdTaiKhoanNavigation { get; set; }
}
