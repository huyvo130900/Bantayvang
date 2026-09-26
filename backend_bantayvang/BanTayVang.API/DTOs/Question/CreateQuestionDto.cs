using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Question
{
    public class CreateQuestionDto
    {
        public int? QuestionCategoryId { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        public string? Difficulty { get; set; }
        public string? Department { get; set; }
        public string? ImageUrl { get; set; }
        public string? SuggestedAnswer { get; set; }

        // Additional properties for enhanced question management
        public string? QuestionCategory { get; set; }
        public string? Level { get; set; }
        
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }
}