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

        // OBSOLETE: kept only so old rows/queries against a single department keep working during
        // migration. New code must read/write eligibility via ExamCampaignDepartments instead -
        // this column no longer drives who can see or manage the campaign (see
        // ExamCampaignService, ExamCampaignController, ExamController, ExamService,
        // StatisticsController, ExamSubmissionService).
        [Obsolete("Use ExamCampaignDepartments instead - a campaign can now be scoped to 1-n departments.")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public virtual ICollection<ExamCampaignDepartment> ExamCampaignDepartments { get; set; } = new List<ExamCampaignDepartment>();

        /// <summary>
        /// "Department" (mặc định, dùng ExamCampaignDepartments) hoặc "AssignedList" (chỉ những
        /// người có ExamAssignment active với 1 trong các ExamPaper của kỳ thi này mới được thi -
        /// xem ExamValidationService.ValidateStartExamAsync). 2 cơ chế độc lập, không kết hợp.
        /// </summary>
        [StringLength(20)]
        public string AccessMode { get; set; } = "Department";

        [StringLength(50)]
        public string Status { get; set; } = "DangChuanBi"; // DangChuanBi, DangDienRa, TamDung, DaKetThuc

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? OrganizedBy { get; set; }

        public int? MinPassQuestions { get; set; }

        public int? TotalQuestions { get; set; }

        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Kỳ thi luyện tập tồn tại vĩnh viễn cho học viên tự luyện, KHÔNG phải kỳ thi thật có
        /// thời hạn: ExamValidationService.ValidateStartExamAsync bỏ qua StartTime/EndTime/giới
        /// hạn "đã làm rồi không được làm lại" khi cờ này bật, và mọi thống kê đạt/không đạt chính
        /// thức (StatisticsService, GradingService.GetResultsByExamCampaignAsync) loại trừ các kỳ
        /// thi có cờ này. Admin/DeptManager tạo 1 lần, học viên vào luyện tập bất cứ lúc nào.
        /// </summary>
        public bool IsPracticeMode { get; set; } = false;
    }
}
