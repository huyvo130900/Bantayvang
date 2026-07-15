using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Category
{

    /// <summary>
    /// Question Type DTO
    /// </summary>
    public class LoaicauhoiDto
    {
        public int Id { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public int TotalQuestions { get; set; }
    }

    /// <summary>
    /// Create/Update Question Type DTO
    /// </summary>
    public class CreateLoaicauhoiDto
    {
        [Required(ErrorMessage = "Tên loại câu hỏi không được để trống")]
        [StringLength(100, ErrorMessage = "Tên loại tối đa 100 ký tự")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }
    }
}