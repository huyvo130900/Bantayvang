using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Question
{
    public class CreateCauhoiDto
    {
        public int? IdLoaiCauHoi { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string NoiDung { get; set; } = string.Empty;
        
        public string? DoKho { get; set; }
        public string? KhoaPhong { get; set; }
        public string? HinhAnh { get; set; }

        // Additional properties for enhanced question management
        public string? LoaiCauHoi { get; set; }
        public string? MucDo { get; set; }
        
        public List<CreateLuachonDto> DanhSachLuaChon { get; set; } = new();
    }
}