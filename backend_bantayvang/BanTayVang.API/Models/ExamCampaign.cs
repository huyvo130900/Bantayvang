using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Kỳ thi - chứa nhiều đề thi và ca thi
    /// Ví dụ: "Kỳ thi Bàn tay vàng Q2/2026", "Kiểm soát nhiễm khuẩn 2026"
    /// </summary>
    public class ExamCampaign
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CampaignCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string CampaignName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "DangChuanBi"; // DangChuanBi, DangDienRa, TamDung, DaKetThuc

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? OrganizedBy { get; set; }

        public int? MinPassQuestions { get; set; }

        public int? TotalQuestions { get; set; }

        public int? DurationMinutes { get; set; }
    }
}