using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class SubmitExamDto
    {
        [Required]
        public int ExamSubmissionId { get; set; }
        
        public List<SubmitAnswerDto> DanhSachCauTraLoi { get; set; } = new();
    }
}