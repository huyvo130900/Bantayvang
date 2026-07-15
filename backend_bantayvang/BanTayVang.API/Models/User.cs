using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class User
{
    public int Id { get; set; }

    public string? EmployeeCode { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public int? RoleId { get; set; }

    public bool? Status { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// FK -> ExamRegistrations.Id - chỉ dùng cho role DeptManager (ID=5)
    /// </summary>
    public int? DeptManagerDeptId { get; set; }

    [ForeignKey("DeptManagerDeptId")]
    public virtual Department? ManagedDepartment { get; set; }

    public virtual ICollection<ExamSubmission> ExamSubmissions { get; set; } = new List<ExamSubmission>();

    public virtual ICollection<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
