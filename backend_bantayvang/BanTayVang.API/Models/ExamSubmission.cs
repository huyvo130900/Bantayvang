namespace BanTayVang.API.Models;

public partial class ExamSubmission
{
    public int Id { get; set; }
    public int? ExamPaperId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? SubmitTime { get; set; }
    public double? TotalScore { get; set; }
    public string? Status { get; set; }
    public int? CorrectAnswers { get; set; }
    public int? TotalQuestions { get; set; }
    public string? DanhGiaKhoa { get; set; }

    /// <summary>
    /// Công bố điểm riêng cho thí sinh này (không phụ thuộc IsResultPublished của đề thi).
    /// Nếu true → thí sinh thấy điểm dù IsResultPublished của ExamPaper = false.
    /// </summary>
    public bool CongBoRieng { get; set; } = false;

    /// <summary>Thời điểm công bố riêng</summary>
    public DateTime? ThoiGianCongBoRieng { get; set; }

    /// <summary>Người thực hiện công bố riêng (Admin/DeptManager)</summary>
    public int? NguoiCongBoRieng { get; set; }

    /// <summary>Mã đề thi (cache từ ExamPaper.ExamPaperCode)</summary>
    public string? ExamPaperCode { get; set; }

    /// <summary>FK → ExamCampaign — kỳ thi mà bài thi này thuộc về</summary>
    public int? ExamCampaignId { get; set; }
    public virtual ExamCampaign? KyThiNavigation { get; set; }

    /// <summary>Tổng số cảnh báo gian lận trong bài thi này</summary>
    public int? TongSoCanhBao { get; set; }

    public virtual ExamPaper? IdDeThiNavigation { get; set; }
    public virtual User? IdTaiKhoanNavigation { get; set; }
    public virtual ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();
    public virtual ICollection<CheatWarning> CheatWarnings { get; set; } = new List<CheatWarning>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
