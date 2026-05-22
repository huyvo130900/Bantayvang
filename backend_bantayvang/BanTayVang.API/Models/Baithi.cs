using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Baithi
{
    public int Id { get; set; }

    public int? IdTaiKhoan { get; set; }

    public int? IdDeThi { get; set; }

    public string? TrangThai { get; set; }

    public string? MaDeThi { get; set; }

    public DateTime? ThoiGianNop { get; set; }

    public double? TongDiem { get; set; }

    public int? SoCauDung { get; set; }

    public int? TongSoCau { get; set; }

    public int? TongSoCanhBao { get; set; }

    // Additional fields for exam session management
    public DateTime? ThoiGianBatDau { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public string? LyDoKetThuc { get; set; }

    public virtual ICollection<Canhbaogianlan> Canhbaogianlans { get; set; } = new List<Canhbaogianlan>();

    public virtual ICollection<Chitietlambai> Chitietlambais { get; set; } = new List<Chitietlambai>();

    public virtual Dethi? IdDeThiNavigation { get; set; }

    public virtual Taikhoan? IdTaiKhoanNavigation { get; set; }

    public virtual ICollection<Logthaotac> Logthaotacs { get; set; } = new List<Logthaotac>();
}
