using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.KyThi
{
    public class ExamGenerationConfigDto
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng đề cần tạo từ 1 đến 100")]
        public int SoLuongDe { get; set; } = 1;

        [Required]
        [Range(1, 200, ErrorMessage = "Số câu hỏi mỗi đề từ 1 đến 200")]
        public int TongSoCau { get; set; }

        [Required]
        public int SoCauMC { get; set; }

        [Required]
        public int SoCauEssay { get; set; }

        [Required]
        public int SoCauEasy { get; set; }

        [Required]
        public int SoCauMedium { get; set; }

        [Required]
        public int SoCauHard { get; set; }

        public string? KhoaPhong { get; set; }
    }

    public class ExamCheckResultDto
    {
        public bool CanGenerate { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
    }
}
