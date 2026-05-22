using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Danhmucauhoi
{
    public int Id { get; set; }

    public string? TenDanhMuc { get; set; }

    public string? Mota { get; set; }

    public virtual ICollection<Cauhoi> Cauhois { get; set; } = new List<Cauhoi>();
}
