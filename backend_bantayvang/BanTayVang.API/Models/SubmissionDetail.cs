using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class SubmissionDetail
{
    public int Id { get; set; }

    public int? ExamSubmissionId { get; set; }

    public int? QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public DateTime? ThoiGianTraLoi { get; set; }

    public bool? DaLuu { get; set; }

    public string? CauTraLoiTuLuan { get; set; }

    public double? ScoreObtained { get; set; }

    public virtual ExamSubmission? IdBaiThiNavigation { get; set; }

    public virtual Question? IdCauHoiNavigation { get; set; }

    public virtual QuestionOption? IdLuaChonDaChonNavigation { get; set; }
}
