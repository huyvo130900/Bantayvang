using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class QuestionCategory
{
    public int Id { get; set; }

    public string? CategoryName { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
