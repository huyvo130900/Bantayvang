using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class User
{
    public int Id { get; set; }

    public string? MaNhanVien { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? ChucDanh { get; set; }

    public string? Department { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public int? RoleId { get; set; }

    public bool? Status { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LanDangNhapCuoi { get; set; }

    /// <summary>
    /// FK -> ExamRegistrations.Id - chỉ dùng cho role DeptManager (ID=5)
    /// </summary>
    public int? DeptManagerDeptId { get; set; }

    [ForeignKey("DeptManagerDeptId")]
    public virtual Department? ManagedDepartment { get; set; }

    public virtual ICollection<ExamSubmission> ExamSubmissions { get; set; } = new List<ExamSubmission>();

    public virtual ICollection<Phiendangnhap> Phiendangnhaps { get; set; } = new List<Phiendangnhap>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
