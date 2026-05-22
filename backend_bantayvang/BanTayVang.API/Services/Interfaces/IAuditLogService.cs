namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Audit log service - tracks all user actions
    /// OWASP A09: Security Logging and Monitoring
    /// </summary>
    public interface IAuditLogService
    {
        Task LogActionAsync(
            string actionType,
            string description,
            int? userId = null,
            int? baithiId = null,
            string? ipAddress = null,
            string? userAgent = null);

        Task<List<AuditLogEntry>> GetUserLogsAsync(int userId, int top = 100);
        Task<List<AuditLogEntry>> GetExamSessionLogsAsync(int baithiId);
        Task<List<AuditLogEntry>> GetRecentLogsAsync(int top = 200);
        Task<List<AuditLogEntry>> SearchLogsAsync(string? actionType, DateTime? from, DateTime? to);
    }

    public class AuditLogEntry
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public int? BaithiId { get; set; }
        public string? ActionType { get; set; }
        public string? Description { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}