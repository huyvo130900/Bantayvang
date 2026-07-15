using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class UserRole
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? RoleId { get; set; }

    public virtual User? IdTaiKhoanNavigation { get; set; }

    public virtual Role? IdVaiTroNavigation { get; set; }
}
