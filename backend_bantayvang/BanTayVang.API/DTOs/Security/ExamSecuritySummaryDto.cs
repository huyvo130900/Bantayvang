using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.AntiCheat
{
    /// <summary>
    /// Security summary for exam session
    /// Follows OWASP logging standards
    /// </summary>
    public class ExamSecuritySummaryDto
    {
        public int ExamSessionId { get; set; }
        public int BaithiId { get; set; }
        public int TotalWarnings { get; set; }
        public int CriticalWarnings { get; set; }
        public int HighWarnings { get; set; }
        public int MediumWarnings { get; set; }
        public int LowWarnings { get; set; }
        public SecurityRiskLevel RiskLevel { get; set; }
        public bool RecommendTermination { get; set; }
        public DateTime? LastWarningTime { get; set; }
        public string SecurityStatus { get; set; } = string.Empty;
        public List<SecurityEventDto> RecentEvents { get; set; } = new();
    }

    /// <summary>
    /// Security event with OWASP compliance
    /// </summary>
    public class SecurityEventDto
    {
        [Required]
        public string EventType { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public SecuritySeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        
        // OWASP: Don't log sensitive data
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        
        // Additional context (sanitized)
        public Dictionary<string, string> Context { get; set; } = new();
    }

    public enum SecurityRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum SecuritySeverity
    {
        Info = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }
}