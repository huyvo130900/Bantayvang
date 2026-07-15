namespace BanTayVang.API.DTOs.Question
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public int? QuestionCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Content { get; set; }
        public string? Difficulty { get; set; }
        public string? Department { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
        public List<string> Campaigns { get; set; } = new();
        public List<string> ExamPapers { get; set; } = new();
    }
}