using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.ExamCampaign
{
    public class ExamCampaignDto
    {
        public int Id { get; set; }
        public string? CampaignCode { get; set; }
        public string? CampaignName { get; set; }
        public string? Description { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? OrganizedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SoLuongDeThi { get; set; }
        public int TongThiSinh { get; set; }
        public List<string> DanhSachMaDeThi { get; set; } = new();
        public string? ExamPaperCode { get; set; }
        public int? MinPassQuestions { get; set; }
        public int? TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class CreateKyThiDto
    {
        [Required(ErrorMessage = "Mã kỳ thi là bắt buộc")]
        [StringLength(50)]
        public string CampaignCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kỳ thi là bắt buộc")]
        [StringLength(255)]
        public string CampaignName { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime? EndTime { get; set; }

        public string? OrganizedBy { get; set; }
        public int? MinPassQuestions { get; set; }
        public int? TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class UpdateKyThiDto : CreateKyThiDto
    {
        public string Status { get; set; } = "DangChuanBi";
    }
}