using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class DethiCauhoi
{
    public int Id { get; set; }

    public int? IdDeThi { get; set; }

    public int? IdCauHoi { get; set; }

    public double? TrongSo { get; set; }

    public virtual Cauhoi? IdCauHoiNavigation { get; set; }

    public virtual Dethi? IdDeThiNavigation { get; set; }
}
