using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class ExamPaperQuestion
{
    public int Id { get; set; }

    public int? ExamPaperId { get; set; }

    public int? QuestionId { get; set; }

    public double? TrongSo { get; set; }

    public virtual Question? IdCauHoiNavigation { get; set; }

    public virtual ExamPaper? IdDeThiNavigation { get; set; }
}
