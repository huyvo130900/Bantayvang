using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class AuditLogService : IAuditLogService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(BanTayVangDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(
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
            string? department = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Username = username,
                    ActionType = actionType?.Length > 100 ? actionType.Substring(0, 100) : actionType,
                    Detail = description?.Length > 4000 ? description.Substring(0, 4000) : description,
                    HttpMethod = method,
                    ApiPath = path?.Length > 500 ? path.Substring(0, 500) : path,
                    HttpStatusCode = statusCode,
                    ActionTime = DateTime.UtcNow.AddHours(7),
                    ExamSubmissionId = examSubmissionId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                    Department = department
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log: {ActionType}", actionType);
            }
        }

        public async Task<List<AuditLogEntry>> GetRecentLogsAsync(int top = 500, string? department = null)
        {
            var query = _context.AuditLogs.Include(l => l.User).AsQueryable();
            if (!string.IsNullOrEmpty(department))
                query = query.Where(l => l.Department == department);

            return await query
                .OrderByDescending(l => l.ActionTime)
                .Take(top)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetUserLogsAsync(int userId, int top = 100, string? department = null)
        {
            var query = _context.AuditLogs.Where(l => l.UserId == userId);
            if (!string.IsNullOrEmpty(department))
                query = query.Where(l => l.Department == department);

            return await query
                .OrderByDescending(l => l.ActionTime)
                .Take(top)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetExamSessionLogsAsync(int examSubmissionId, string? department = null)
        {
            var query = _context.AuditLogs.Where(l => l.ExamSubmissionId == examSubmissionId);
            if (!string.IsNullOrEmpty(department))
                query = query.Where(l => l.Department == department);

            return await query
                .OrderByDescending(l => l.ActionTime)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> SearchLogsAsync(
            string? actionType = null,
            string? username = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50,
            string? department = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(department))
                query = query.Where(l => l.Department == department);

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(l => l.ActionType != null && l.ActionType.Contains(actionType));

            if (!string.IsNullOrEmpty(username))
                query = query.Where(l => l.Username != null && l.Username.Contains(username));

            if (from.HasValue)
                query = query.Where(l => l.ActionTime >= from);

            if (to.HasValue)
                query = query.Where(l => l.ActionTime <= to.Value.AddDays(1));

            return await query
                .OrderByDescending(l => l.ActionTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? actionType = null, string? username = null, DateTime? from = null, DateTime? to = null, string? department = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(department))
                query = query.Where(l => l.Department == department);
            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(l => l.ActionType != null && l.ActionType.Contains(actionType));
            if (!string.IsNullOrEmpty(username))
                query = query.Where(l => l.Username != null && l.Username.Contains(username));
            if (from.HasValue) query = query.Where(l => l.ActionTime >= from);
            if (to.HasValue) query = query.Where(l => l.ActionTime <= to.Value.AddDays(1));
            return await query.CountAsync();
        }

        private static AuditLogEntry MapToEntry(AuditLog l) => new()
        {
            Id = l.Id,
            UserId = l.UserId,
            Username = l.Username ?? l.User?.Username,
            ExamSubmissionId = l.ExamSubmissionId,
            ActionType = l.ActionType,
            Method = l.HttpMethod,
            Path = l.ApiPath,
            StatusCode = l.HttpStatusCode,
            Description = l.Detail,
            Timestamp = l.ActionTime,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            Department = l.Department
        };
    }
}

