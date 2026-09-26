namespace BanTayVang.API.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogActionAsync(
            string actionType,
            string description,
            int? userId = null,
            string? username = null,
            int? examSubmissionId = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? method = null,
            string? path = null,
            int? statusCode = null,
            string? department = null);

        Task<List<AuditLogEntry>> GetUserLogsAsync(int userId, int top = 100, string? department = null);
        Task<List<AuditLogEntry>> GetExamSessionLogsAsync(int examSubmissionId, string? department = null);
        Task<List<AuditLogEntry>> GetRecentLogsAsync(int top = 500, string? department = null);
        Task<List<AuditLogEntry>> SearchLogsAsync(
            string? actionType = null,
            string? username = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50,
            string? department = null);
        Task<int> GetTotalCountAsync(string? actionType = null, string? username = null, DateTime? from = null, DateTime? to = null, string? department = null);
    }

    public class AuditLogEntry
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public int? ExamSubmissionId { get; set; }
        public string? ActionType { get; set; }
        /// <summary>HTTP Method: GET, POST, PUT, DELETE</summary>
        public string? Method { get; set; }
        /// <summary>API Path</summary>
        public string? Path { get; set; }
        public int? StatusCode { get; set; }
        public string? Description { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Department { get; set; }
    }
}
