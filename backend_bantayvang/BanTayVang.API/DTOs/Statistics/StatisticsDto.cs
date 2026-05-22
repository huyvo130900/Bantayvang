namespace BanTayVang.API.DTOs.Statistics
{
    /// <summary>
    /// Dashboard overview statistics
    /// </summary>
    public class DashboardDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalExams { get; set; }
        public int ActiveExams { get; set; }
        public int TotalSubmissions { get; set; }
        public int InProgressExams { get; set; }
        public int CompletedExams { get; set; }
        public double AverageScore { get; set; }
        public int TotalCheatingWarnings { get; set; }
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Username { get; set; }
    }

    /// <summary>
    /// Exam statistics
    /// </summary>
    public class ExamStatisticsDto
    {
        public int ExamId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public int TotalParticipants { get; set; }
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public double PassRate { get; set; }
        public List<ScoreDistributionDto> ScoreDistribution { get; set; } = new();
    }

    public class ScoreDistributionDto
    {
        public string Range { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// User exam history
    /// </summary>
    public class UserExamHistoryDto
    {
        public int BaiThiId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianNop { get; set; }
        public string? TrangThai { get; set; }
        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public double? TongDiem { get; set; }
        public int? SoCanhBao { get; set; }
    }

    /// <summary>
    /// Top performers
    /// </summary>
    public class TopPerformerDto
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? KhoaPhong { get; set; }
        public int ExamsTaken { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
    }
}