using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.ExamCampaign
{
    public class ExamCampaignDto
    {
        public int Id { get; set; }
        public string? CampaignCode { get; set; }
        public string? CampaignName { get; set; }
        public string? Description { get; set; }
        public List<int> DepartmentIds { get; set; } = new();
        public List<string> DepartmentNames { get; set; } = new();
        public string AccessMode { get; set; } = "Department";
        public bool IsPracticeMode { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? OrganizedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalExamPapers { get; set; }
        public int TotalCandidates { get; set; }
        public List<string> ExamPaperCodes { get; set; } = new();
        public string? ExamPaperCode { get; set; }
        public int? MinPassQuestions { get; set; }
        public int? TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class CreateExamCampaignDto
    {
        [Required(ErrorMessage = "Mã kỳ thi là bắt buộc")]
        [StringLength(50)]
        public string CampaignCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kỳ thi là bắt buộc")]
        [StringLength(255)]
        public string CampaignName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Danh sách khoa được phép thấy/thi kỳ thi này (1-n). Rỗng/null = không giới hạn khoa
        // (giữ hành vi cũ "Tất cả khoa" khi kỳ thi không gán khoa nào). Chỉ có tác dụng khi
        // AccessMode = "Department".
        public List<int>? DepartmentIds { get; set; }

        // "Department" (mặc định) hoặc "AssignedList" - 2 cơ chế độc lập, không kết hợp. Khi
        // "AssignedList", eligibility do ExamAssignment quyết định (xem
        // POST /api/ExamCampaign/{id}/assign-from-excel), DepartmentIds bị bỏ qua.
        [RegularExpression("^(Department|AssignedList)$", ErrorMessage = "AccessMode không hợp lệ")]
        public string AccessMode { get; set; } = "Department";

        // Kỳ thi luyện tập tồn tại vĩnh viễn, không có thời hạn thật - xem ExamCampaign.IsPracticeMode.
        public bool IsPracticeMode { get; set; } = false;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime? EndTime { get; set; }

        public string? OrganizedBy { get; set; }

        // BUG FIX: these had no server-side range check at all - only the frontend form
        // (ky-thi/schemas.ts) validated them, so anyone calling the API directly (Postman,
        // script, a future alternate client) could set e.g. DurationMinutes to a negative
        // number. That breaks IsExamExpired's math (examEndTime ends up BEFORE the exam even
        // starts), causing AutoSubmitExpiredExamsJob to auto-submit the session the instant a
        // student begins. Mirrors the same limits already enforced on ExamGenerationConfigDto.
        [Range(0, 200, ErrorMessage = "Số câu đúng tối thiểu phải từ 0 đến 200")]
        public int? MinPassQuestions { get; set; }

        [Range(1, 200, ErrorMessage = "Tổng số câu hỏi phải từ 1 đến 200")]
        public int? TotalQuestions { get; set; }

        [Range(1, 1440, ErrorMessage = "Thời gian làm bài phải từ 1 đến 1440 phút")]
        public int? DurationMinutes { get; set; }
    }

    public class UpdateExamCampaignDto : CreateExamCampaignDto
    {
        public string Status { get; set; } = "DangChuanBi";
    }

    /// <summary>
    /// 1 dòng trong báo cáo "ai đủ điều kiện thi / ai đã thi" của 1 kỳ thi (GET
    /// /api/ExamCampaign/{id}/eligibility) - gộp roster theo khoa của kỳ thi với kết quả thi
    /// (nếu có) của từng người.
    /// </summary>
    public class ExamCampaignEligibilityDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public bool HasSubmitted { get; set; }
        public string? SubmissionStatus { get; set; }
        public double? TotalScore { get; set; }
        public DateTime? SubmitTime { get; set; }
    }

    /// <summary>
    /// Kết quả của POST /api/ExamCampaign/{id}/assign-from-excel - vì file do người dùng tự soạn
    /// (thường copy/paste từ danh sách nội bộ) nên luôn có khả năng gõ sai/thiếu mã, cần báo cáo
    /// rõ dòng nào không khớp được với tài khoản nào để người upload tự sửa lại.
    /// </summary>
    public class AssignFromExcelResultDto
    {
        public int TotalRows { get; set; }
        public int MatchedUserCount { get; set; }
        public List<string> NotFoundCodes { get; set; } = new();
    }
}
