using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class SubmissionDetail
{
    public int Id { get; set; }

    public int? ExamSubmissionId { get; set; }

    public int? QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public DateTime? AnswerTime { get; set; }

    public bool? IsSaved { get; set; }

    public string? EssayAnswer { get; set; }

    public string? EssayImageUrl { get; set; }
    public double? ScoreObtained { get; set; }

    public string? TeacherComment { get; set; }

    /// <summary>Điểm do AI đề xuất (0, 0.5 hoặc 1)</summary>
    public double? AiScore { get; set; }

    /// <summary>Nhận xét, phân tích do AI sinh ra</summary>
    public string? AiComment { get; set; }

    /// <summary>Trạng thái chấm AI: null=chưa yêu cầu, Pending=đang chờ, Processing=đang chạy, Done=hoàn tất, Error=lỗi</summary>
    public string? AiGradingStatus { get; set; }

    public virtual ExamSubmission? ExamSubmission { get; set; }

    public virtual Question? Question { get; set; }

    public virtual QuestionOption? SelectedOption { get; set; }
}
