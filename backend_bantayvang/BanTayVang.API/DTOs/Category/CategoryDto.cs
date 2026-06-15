using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Category
{

    /// <summary>
    /// Question Type DTO
    /// </summary>
    public class LoaicauhoiDto
    {
        public int Id { get; set; }
        public string? TenLoai { get; set; }
        public string? MoTa { get; set; }
        public int SoCauHoi { get; set; }
    }

    /// <summary>
    /// Create/Update Question Type DTO
    /// </summary>
    public class CreateLoaicauhoiDto
    {
        [Required(ErrorMessage = "Tên loại câu hỏi không được để trống")]
        [StringLength(100, ErrorMessage = "Tên loại tối đa 100 ký tự")]
        public string TenLoai { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? MoTa { get; set; }
    }
}