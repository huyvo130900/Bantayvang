using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class QuestionOption
{
    public int Id { get; set; }

    public int? QuestionId { get; set; }

    public string? Content { get; set; }

    public bool? IsCorrect { get; set; }

    public int? OrderIndex { get; set; }

    public virtual ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();

    public virtual Question? Question { get; set; }
}
