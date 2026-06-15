using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Loaicauhoi
{
    public int Id { get; set; }

    public string? TenLoai { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<Cauhoi> Cauhois { get; set; } = new List<Cauhoi>();
}
