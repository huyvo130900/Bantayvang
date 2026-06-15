using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Luachon
{
    public int Id { get; set; }

    public int? IdCauHoi { get; set; }

    public string? NoiDung { get; set; }

    public bool? LaDapAnDung { get; set; }

    public int? ThuTu { get; set; }

    public virtual ICollection<Chitietlambai> Chitietlambais { get; set; } = new List<Chitietlambai>();

    public virtual Cauhoi? IdCauHoiNavigation { get; set; }
}
