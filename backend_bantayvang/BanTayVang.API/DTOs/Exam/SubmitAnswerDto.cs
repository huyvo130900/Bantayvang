using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class SubmitAnswerDto
    {
        [Required]
        public int ExamSubmissionId { get; set; }
        
        [Required]
        public int QuestionId { get; set; }
        
        public int? SelectedOptionId { get; set; } // Cho câu trắc nghiệm
        public string? CauTraLoiTuLuan { get; set; } // Cho câu tự luận
        public bool DaLuu { get; set; } = true;
    }
}