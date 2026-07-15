namespace BanTayVang.API.DTOs.Question
{
    public class QuestionOptionDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public bool? IsCorrect { get; set; }
        public int? OrderIndex { get; set; }
    }
}