using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class Logthaotac
{
    public int Id { get; set; }

    public int? IdBaiThi { get; set; }

    public string? LoaiThaoTac { get; set; }

    public string? ChiTiet { get; set; }

    public DateTime? ThoiGian { get; set; }

    [Column("DiaChi_IP")]
    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    public virtual Baithi? IdBaiThiNavigation { get; set; }
}
