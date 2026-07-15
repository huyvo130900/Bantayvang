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

    public double? ScoreObtained { get; set; }

    public virtual ExamSubmission? ExamSubmission { get; set; }

    public virtual Question? Question { get; set; }

    public virtual QuestionOption? SelectedOption { get; set; }
}
