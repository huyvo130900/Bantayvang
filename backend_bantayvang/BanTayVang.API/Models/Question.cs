using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Question
{
    public int Id { get; set; }

    public int? QuestionCategoryId { get; set; }

    public string? Content { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Difficulty { get; set; }

    public string? Department { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Đáp án mẫu / gợi ý cho câu Tự luận (không bắt buộc)</summary>
    public string? SuggestedAnswer { get; set; }

    public virtual ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();

    public virtual ICollection<ExamPaperQuestion> ExamPaperQuestions { get; set; } = new List<ExamPaperQuestion>();

    public virtual QuestionCategory? QuestionCategory { get; set; }

    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
}
