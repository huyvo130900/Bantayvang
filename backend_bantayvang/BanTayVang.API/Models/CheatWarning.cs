using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class CheatWarning
{
    public int Id { get; set; }

    public int? ExamSubmissionId { get; set; }

    public string? WarningType { get; set; }

    public string? Description { get; set; }

    public DateTime? ActionTime { get; set; }

    public int? SoLanViPham { get; set; }

    // Additional fields for enhanced security monitoring
    public string? MucDoNghiemTrong { get; set; }

    public string? CorrelationId { get; set; }

    public virtual ExamSubmission? ExamSubmission { get; set; }
}
