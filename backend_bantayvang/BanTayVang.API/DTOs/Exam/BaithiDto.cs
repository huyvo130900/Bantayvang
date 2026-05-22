namespace BanTayVang.API.DTOs.Exam
{
    public class BaithiDto
    {
        public int Id { get; set; }
        public int IdTaiKhoan { get; set; }
        public int IdDeThi { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? ThoiGianNop { get; set; }
        public double? TongDiem { get; set; }
        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public int? TongSoCanhBao { get; set; }
        
        // Thông tin đề thi
        public string? TenDeThi { get; set; }
        public int? ThoiGianLamBai { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        
        // Thời gian còn lại (tính toán)
        public int? ThoiGianConLai { get; set; } // giây
    }
}