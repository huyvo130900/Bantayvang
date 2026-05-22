using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class SubmitExamDto
    {
        [Required]
        public int IdBaiThi { get; set; }
        
        public List<SubmitAnswerDto> DanhSachCauTraLoi { get; set; } = new();
    }
}