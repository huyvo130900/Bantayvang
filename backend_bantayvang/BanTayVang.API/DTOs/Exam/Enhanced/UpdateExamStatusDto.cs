using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class UpdateExamStatusDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(Draft|Active|Inactive|Archived|Published)$", ErrorMessage = "Giá trị trạng thái không hợp lệ")]
        public string Status { get; set; } = string.Empty;
    }
}
