using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Role
{
    public int Id { get; set; }

    public string? RoleCode { get; set; }

    public string? RoleName { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
