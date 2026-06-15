namespace BanTayVang.API.DTOs.Exam
{
    public class BaithiDto
    {
        public int Id { get; set; }
        public int IdTaiKhoan { get; set; }
        public int IdDeThi { get; set; }
        public int? IdKyThi { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? ThoiGianNop { get; set; }

        // Điểm tự tính: (SoCauDung * 10.0 / TongSoCau)
        public double? TongDiem { get; set; }
        public double? DiemSo => TongSoCau > 0 ? Math.Round((SoCauDung ?? 0) * 10.0 / TongSoCau!.Value, 2) : TongDiem;

        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public int? TongSoCanhBao { get; set; }

        // Thông tin đề thi
        public string? TenDeThi { get; set; }
        public string? MaDeThi { get; set; }
        public int? ThoiGianLamBai { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public int? ThoiGianConLai { get; set; } // giây

        // Công bố kết quả
        public bool CongBoKetQua { get; set; } = false;

        // Kết quả đạt/không đạt (backend tính)
        public bool? Pass { get; set; }
    }
}
