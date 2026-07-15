using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string MaKhoa { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Status { get; set; }
        public int? DeptManagerId { get; set; }
        public string? TenQuanLy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required]
        [StringLength(50)]
        public string MaKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool Status { get; set; } = true;
    }

    public class UpdateDepartmentDto
    {
        [Required]
        [StringLength(255)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool Status { get; set; }
    }

    public class AssignManagerDto
    {
        /// <summary>
        /// ID tài khoản có role DeptManager (ID=5)
        /// </summary>
        [Required]
        public int DeptManagerId { get; set; }
    }

    public class ExamVisibilityDto
    {
        [Required]
        public bool IsResultPublished { get; set; }
    }

    public class DepartmentDashboardDto
    {
        public int DeptId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TongSoDeThi { get; set; }
        public int TongSoThiSinh { get; set; }
        public double DiemTrungBinh { get; set; }
        public List<KyThiSummaryDto> KyThiGanDay { get; set; } = new();
    }

    public class KyThiSummaryDto
    {
        public int Id { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SoDeThi { get; set; }
        public int SoThiSinh { get; set; }
    }
}
