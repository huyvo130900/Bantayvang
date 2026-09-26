using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.ExamCampaign
{
    public class ExamGenerationConfigDto
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng đề cần tạo từ 1 đến 100")]
        public int NumberOfExams { get; set; } = 1;

        [Required]
        [Range(1, 200, ErrorMessage = "Số câu hỏi mỗi đề từ 1 đến 200")]
        public int TotalQuestions { get; set; }

        [Required]
        public int MultipleChoiceQuestions { get; set; }

        [Required]
        public int EssayQuestions { get; set; }

        [Required]
        public int EasyQuestions { get; set; }

        [Required]
        public int MediumQuestions { get; set; }

        [Required]
        public int HardQuestions { get; set; }

        public string? Department { get; set; }

        // BUG FIX: a campaign can now be scoped to 1-n departments (ExamCampaignDepartments), but
        // this DTO only ever had a single `Department` string filter - the generate-exams dialog
        // had no way to express "any of these N departments" and silently fell back to "no
        // filter" (pull from the ENTIRE question bank, every department) for 0 or 2+ department
        // campaigns. When populated, this takes priority over `Department` below.
        public List<string>? DepartmentNames { get; set; }
    }

    public class ExamCheckResultDto
    {
        public bool CanGenerate { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
    }
}
