using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class LoginSession
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Token { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? Ip { get; set; }

    public string? ThietBiUserAgent { get; set; }

    public virtual User? User { get; set; }
}
