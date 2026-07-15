using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Question
{
    public class CreateQuestionOptionDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }
}