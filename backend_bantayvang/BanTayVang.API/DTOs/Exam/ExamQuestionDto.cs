namespace BanTayVang.API.DTOs.Exam
{
    public class ExamQuestionDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public int QuestionOrder { get; set; }
        public List<ExamChoiceDto> Options { get; set; } = new();
        
        // Câu trả lời của thí sinh (nếu đã trả lời)
        public int? SelectedOptionId { get; set; }
        public List<int> SelectedOptionIdList { get; set; } = new();
        public string? EssayAnswer { get; set; }
        public bool IsSaved { get; set; }
        public bool AllowMultipleSelection { get; set; }
    }
}