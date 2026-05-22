using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class StartExamDto
    {
        [Required]
        public string MaDeThi { get; set; } = string.Empty;
        
        public string? ThietBiUserAgent { get; set; }
        public string? Ip { get; set; }
    }
}