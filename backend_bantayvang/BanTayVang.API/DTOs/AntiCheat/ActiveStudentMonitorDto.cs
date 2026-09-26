using System;

namespace BanTayVang.API.DTOs.AntiCheat
{
    public class ActiveStudentMonitorDto
    {
        public int ExamSubmissionId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? UserCode { get; set; }
        public string? ExamPaperCode { get; set; }
        public int WarningCount { get; set; }
        public DateTime? StartTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
