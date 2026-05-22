namespace BanTayVang.API.DTOs.Grading
{
    /// <summary>
    /// Result detail of a single exam submission
    /// </summary>
    public class ExamResultDetailDto
    {
        public int BaiThiId { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? KhoaPhong { get; set; }
        public int ExamId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianNop { get; set; }
        public int? DurationMinutes { get; set; }
        public double? TongDiem { get; set; }
        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public string? TrangThai { get; set; }
        public bool Pass { get; set; }
        public int? SoCanhBao { get; set; }
        public List<AnswerDetailDto> Answers { get; set; } = new();
    }

    public class AnswerDetailDto
    {
        public int CauHoiId { get; set; }
        public string? NoiDungCauHoi { get; set; }
        public double? Diem { get; set; }
        public int? IdLuaChonDaChon { get; set; }
        public string? NoiDungDapAn { get; set; }
        public string? CauTraLoiTuLuan { get; set; }
        public bool IsCorrect { get; set; }
        public double? DiemDatDuoc { get; set; }
        public int? IdLuaChonDung { get; set; }
        public string? NoiDungDapAnDung { get; set; }
    }

    /// <summary>
    /// Manual grading for essay questions
    /// </summary>
    public class ManualGradingDto
    {
        public int ChiTietLamBaiId { get; set; }
        public double DiemDatDuoc { get; set; }
        public string? NhanXet { get; set; }
    }

    /// <summary>
    /// Re-grade request
    /// </summary>
    public class RegradeRequestDto
    {
        public int BaiThiId { get; set; }
    }

    /// <summary>
    /// Export result options
    /// </summary>
    public class ExportResultsDto
    {
        public int? ExamId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? KhoaPhong { get; set; }
    }
}