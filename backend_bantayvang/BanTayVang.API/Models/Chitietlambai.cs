using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Chitietlambai
{
    public int Id { get; set; }

    public int? IdBaiThi { get; set; }

    public int? IdCauHoi { get; set; }

    public int? IdLuaChonDaChon { get; set; }

    public DateTime? ThoiGianTraLoi { get; set; }

    public bool? DaLuu { get; set; }

    public string? CauTraLoiTuLuan { get; set; }

    public double? DiemDatDuoc { get; set; }

    public virtual Baithi? IdBaiThiNavigation { get; set; }

    public virtual Cauhoi? IdCauHoiNavigation { get; set; }

    public virtual Luachon? IdLuaChonDaChonNavigation { get; set; }
}
