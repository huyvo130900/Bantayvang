namespace BanTayVang.API.DTOs.Exam
{
    /// <summary>
    /// DTO for multiple-answer questions
    /// </summary>
    public class SubmitMultipleAnswerDto
    {
        public int ExamSubmissionId { get; set; }
        public int QuestionId { get; set; }
        public List<int> SelectedOptionId { get; set; } = new();
        public string? CauTraLoiTuLuan { get; set; }
        public bool DaLuu { get; set; }
    }
}