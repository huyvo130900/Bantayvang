using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class SubmitAnswerDto
    {
        [Required]
        public int IdBaiThi { get; set; }
        
        [Required]
        public int IdCauHoi { get; set; }
        
        public int? IdLuaChonDaChon { get; set; } // Cho câu trắc nghiệm
        public string? CauTraLoiTuLuan { get; set; } // Cho câu tự luận
        public bool DaLuu { get; set; } = true;
    }
}