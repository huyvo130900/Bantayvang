using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class ExamRegistration
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string IdCardNumber { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(255)]
    public string? WorkUnit { get; set; }

    [MaxLength(255)]
    public string? Major { get; set; }

    public int? DepartmentId { get; set; }

    [MaxLength(255)]
    public string? ExamPurpose { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow.AddHours(7);

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int? ApproverId { get; set; }

    public DateTime? ApprovalDate { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
}

