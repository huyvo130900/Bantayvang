using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Vaitro
{
    public int Id { get; set; }

    public string? MaVaiTro { get; set; }

    public string? TenVaiTro { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<TaikhoanVaitro> TaikhoanVaitros { get; set; } = new List<TaikhoanVaitro>();
}
