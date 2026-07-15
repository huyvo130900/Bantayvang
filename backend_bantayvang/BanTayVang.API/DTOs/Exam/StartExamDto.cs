using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class StartExamDto
    {
        [Required]
        public string ExamPaperCode { get; set; } = string.Empty;
        
        public int? KyThiId { get; set; }
        public string? ThietBiUserAgent { get; set; }
        public string? Ip { get; set; }
    }
}