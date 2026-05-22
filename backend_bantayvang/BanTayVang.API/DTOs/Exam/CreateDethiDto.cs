using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class CreateDethiDto
    {
        [Required]
        [StringLength(50)]
        public string MaDeThi { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string TenDeThi { get; set; } = string.Empty;
        
        [Range(1, 300)]
        public int ThoiGianLamBai { get; set; } // phút
        
        public DateTime ThoiGianBatDau { get; set; }
        public string? TrangThai { get; set; } = "Draft";
        
        public List<int> DanhSachIdCauHoi { get; set; } = new();
    }
}