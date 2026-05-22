using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Canhbaogianlan
{
    public int Id { get; set; }

    public int? IdBaiThi { get; set; }

    public string? LoaiCanhBao { get; set; }

    public string? MoTa { get; set; }

    public DateTime? ThoiGian { get; set; }

    public int? SoLanViPham { get; set; }

    // Additional fields for enhanced security monitoring
    public string? MucDoNghiemTrong { get; set; }

    public string? CorrelationId { get; set; }

    public virtual Baithi? IdBaiThiNavigation { get; set; }
}
