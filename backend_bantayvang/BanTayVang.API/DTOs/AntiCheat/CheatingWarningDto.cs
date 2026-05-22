using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.AntiCheat
{
    public class CheatingWarningDto
    {
        [Required]
        public int IdBaiThi { get; set; }
        
        [Required]
        public string LoaiCanhBao { get; set; } = string.Empty; // "TAB_SWITCH", "COPY_PASTE", "RIGHT_CLICK", "FULLSCREEN_EXIT"
        
        public string? MoTa { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}