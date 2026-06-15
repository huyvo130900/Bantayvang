using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Question
{
    public class CreateLuachonDto
    {
        [Required]
        public string NoiDung { get; set; } = string.Empty;
        
        public bool LaDapAnDung { get; set; }
        public int ThuTu { get; set; }
    }
}