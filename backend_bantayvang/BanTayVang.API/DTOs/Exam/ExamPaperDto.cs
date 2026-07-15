using BanTayVang.API.DTOs.Question;

namespace BanTayVang.API.DTOs.Exam
{
    public class ExamPaperDto
    {
        public int Id { get; set; }
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public int? DurationMinutes { get; set; }
        public double? TotalScore { get; set; }
        public DateTime? StartTime { get; set; }
        public string? LinkTruyCap { get; set; }
        public string? Status { get; set; }
        public string? Department { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int TotalQuestions { get; set; }

        public bool IsResultPublished { get; set; } = false;
        public DateTime? PublishedAt { get; set; }
        public int? ExamCampaignId { get; set; }

        /// <summary>
        /// Số câu đúng tối thiểu để đạt (override từ ExamCampaign nếu đề thi có cấu hình riêng)
        /// </summary>
        public int? MinPassQuestions { get; set; }

        public List<QuestionDto> DanhSachCauHoi { get; set; } = new();
    }

    public class ExamPreviewDto
    {
        public int Id { get; set; }
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Status { get; set; }
        public string? Department { get; set; }
        public bool IsResultPublished { get; set; }
        public List<QuestionPreviewDto> Questions { get; set; } = new();
    }

    public class QuestionPreviewDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public string? ChuDe { get; set; }
        public List<ChoicePreviewDto> QuestionOptions { get; set; } = new();
    }

    public class ChoicePreviewDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public bool? IsCorrect { get; set; }
    }
}
