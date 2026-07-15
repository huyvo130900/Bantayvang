using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.AntiCheat
{
    public class CheatingWarningDto
    {
        [Required]
        public int ExamSubmissionId { get; set; }
        
        [Required]
        public string WarningType { get; set; } = string.Empty; // "TAB_SWITCH", "COPY_PASTE", "RIGHT_CLICK", "FULLSCREEN_EXIT"
        
        public string? Description { get; set; }
        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}