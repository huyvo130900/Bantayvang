namespace BanTayVang.API.DTOs.Question
{
    public class QuestionFilterDto
    {
        public int? QuestionCategoryId { get; set; }
        public string? Difficulty { get; set; }
        public string? Department { get; set; }
        public string? SearchKeyword { get; set; }
        public bool? ShowDuplicatesOnly { get; set; }
        public int? ExamCampaignId { get; set; }
        public int? DeThiId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}