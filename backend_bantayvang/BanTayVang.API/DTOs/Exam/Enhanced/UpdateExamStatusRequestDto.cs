using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class UpdateExamStatusRequestDto
    {
        [Required(ErrorMessage = "Exam ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(Draft|Active|Inactive|Archived|Published)$", ErrorMessage = "Giá trị trạng thái không hợp lệ")]
        public string Status { get; set; } = string.Empty;
    }
}
