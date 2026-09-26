using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Status { get; set; }
        public int? DeptManagerId { get; set; }
        public string? ManagerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required]
        [StringLength(50)]
        public string DeptCode { get; set; } = string.Empty;

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
        public int TotalExams { get; set; }
        public int TotalCandidates { get; set; }
        public double AverageScore { get; set; }
        public List<ExamCampaignSummaryDto> RecentCampaigns { get; set; } = new();
    }

    public class ExamCampaignSummaryDto
    {
        public int Id { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ExamCount { get; set; }
        public int CandidateCount { get; set; }
    }
}
