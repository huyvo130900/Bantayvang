using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class TaikhoanVaitro
{
    public int Id { get; set; }

    public int? IdTaiKhoan { get; set; }

    public int? IdVaiTro { get; set; }

    public virtual Taikhoan? IdTaiKhoanNavigation { get; set; }

    public virtual Vaitro? IdVaiTroNavigation { get; set; }
}
