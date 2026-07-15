using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class ExamPaper
{
    public int Id { get; set; }

    public string? ExamPaperCode { get; set; }

    public string? ExamPaperName { get; set; }

    public int? DurationMinutes { get; set; }

    public double? TotalScore { get; set; }

    public DateTime? StartTime { get; set; }

    public string? LinkTruyCap { get; set; }

    public string? Status { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ChecksumData { get; set; }
    public string? Department { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }



    /// <summary>
    /// Công bố kết quả cho thí sinh xem hay không. Mặc định FALSE.
    /// Department Manager / Admin bật toggle này per đề thi.
    /// </summary>
    public bool IsResultPublished { get; set; } = false;

    /// <summary>
    /// ID người bật/tắt công bố kết quả
    /// </summary>
    public int? NguoiCongBo { get; set; }

    /// <summary>
    /// Thời điểm công bố kết quả
    /// </summary>
    public DateTime? ThoiGianCongBo { get; set; }

    public int? KyThiId { get; set; }
    public virtual ExamCampaign? KyThiNavigation { get; set; }

    /// <summary>
    /// Số câu đúng tối thiểu để đạt cho đề thi này (override riêng, không phụ thuộc vào ExamCampaign)
    /// </summary>
    public int? MinPassQuestions { get; set; }

    public virtual ICollection<ExamSubmission> ExamSubmissions { get; set; } = new List<ExamSubmission>();

    public virtual ICollection<ExamPaperQuestion> ExamPaperQuestions { get; set; } = new List<ExamPaperQuestion>();
}
