using BanTayVang.API.DTOs.Question;

namespace BanTayVang.API.DTOs.Exam
{
    public class DethiDto
    {
        public int Id { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public int? ThoiGianLamBai { get; set; } // phút
        public double? TongDiem { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public string? LinkTruyCap { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
        public int SoCauHoi { get; set; }
        public List<CauhoiDto> DanhSachCauHoi { get; set; } = new();
    }
}