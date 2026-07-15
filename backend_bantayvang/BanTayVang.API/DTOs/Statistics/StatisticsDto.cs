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
        public int KyThiId { get; set; }
        public string? CampaignCode { get; set; }
        public string? CampaignName { get; set; }
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
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string? Status { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public double? TotalScore { get; set; }
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
        public string? Department { get; set; }
        public int ExamsTaken { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
    }
}