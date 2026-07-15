namespace BanTayVang.API.DTOs.AntiCheat
{
    public class ExamMonitoringDto
    {
        public int ExamSubmissionId { get; set; }
        public int TongSoCanhBao { get; set; }
        public List<CanhbaoDto> DanhSachCanhBao { get; set; } = new();
        public bool QuaGioiHanCanhBao { get; set; } // > 5 cảnh báo
    }

    public class CanhbaoDto
    {
        public string? LoaiCanhBao { get; set; }
        public string? Description { get; set; }
        public DateTime? ActionTime { get; set; }
    }
}