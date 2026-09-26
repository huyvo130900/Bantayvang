namespace BanTayVang.API.DTOs.AntiCheat
{
    public class ExamMonitoringDto
    {
        public int ExamSubmissionId { get; set; }
        public int TotalWarnings { get; set; }
        public List<WarningDto> WarningsList { get; set; } = new();
        public bool ExceededWarningLimit { get; set; } // > 5 cảnh báo
    }

    public class WarningDto
    {
        public string? WarningType { get; set; }
        public string? Description { get; set; }
        public DateTime? ActionTime { get; set; }
    }
}