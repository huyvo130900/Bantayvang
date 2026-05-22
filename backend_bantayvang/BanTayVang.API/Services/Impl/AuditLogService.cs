using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Audit log implementation using LOGTHAOTAC table
    /// OWASP A09: Security Logging and Monitoring
    /// </summary>
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
            int? baithiId = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                // Truncate to fit DB column (UserAgent might be long)
                var truncatedDetail = string.IsNullOrEmpty(description) ? "" 
                    : (description.Length > 4000 ? description.Substring(0, 4000) : description);
                var truncatedUserAgent = string.IsNullOrEmpty(userAgent) ? "" 
                    : (userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent);

                var log = new Logthaotac
                {
                    LoaiThaoTac = actionType,
                    ChiTiet = userId.HasValue ? $"[User:{userId}] {truncatedDetail}" : truncatedDetail,
                    ThoiGian = DateTime.Now,
                    IdBaiThi = baithiId,
                    DiaChiIp = ipAddress,
                    UserAgent = truncatedUserAgent
                };

                _context.Logthaotacs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't throw - audit log failures shouldn't break the operation
                _logger.LogError(ex, "Failed to write audit log: {ActionType}", actionType);
            }
        }

        public async Task<List<AuditLogEntry>> GetUserLogsAsync(int userId, int top = 100)
        {
            return await _context.Logthaotacs
                .Where(l => l.ChiTiet != null && l.ChiTiet.Contains($"[User:{userId}]"))
                .OrderByDescending(l => l.ThoiGian)
                .Take(top)
                .Select(l => new AuditLogEntry
                {
                    Id = l.Id,
                    UserId = userId,
                    BaithiId = l.IdBaiThi,
                    ActionType = l.LoaiThaoTac,
                    Description = l.ChiTiet,
                    Timestamp = l.ThoiGian,
                    IpAddress = l.DiaChiIp,
                    UserAgent = l.UserAgent
                })
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetExamSessionLogsAsync(int baithiId)
        {
            return await _context.Logthaotacs
                .Where(l => l.IdBaiThi == baithiId)
                .OrderByDescending(l => l.ThoiGian)
                .Select(l => new AuditLogEntry
                {
                    Id = l.Id,
                    BaithiId = l.IdBaiThi,
                    ActionType = l.LoaiThaoTac,
                    Description = l.ChiTiet,
                    Timestamp = l.ThoiGian,
                    IpAddress = l.DiaChiIp,
                    UserAgent = l.UserAgent
                })
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetRecentLogsAsync(int top = 200)
        {
            return await _context.Logthaotacs
                .OrderByDescending(l => l.ThoiGian)
                .Take(top)
                .Select(l => new AuditLogEntry
                {
                    Id = l.Id,
                    BaithiId = l.IdBaiThi,
                    ActionType = l.LoaiThaoTac,
                    Description = l.ChiTiet,
                    Timestamp = l.ThoiGian,
                    IpAddress = l.DiaChiIp,
                    UserAgent = l.UserAgent
                })
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> SearchLogsAsync(string? actionType, DateTime? from, DateTime? to)
        {
            var query = _context.Logthaotacs.AsQueryable();

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(l => l.LoaiThaoTac == actionType);

            if (from.HasValue)
                query = query.Where(l => l.ThoiGian >= from);

            if (to.HasValue)
                query = query.Where(l => l.ThoiGian <= to);

            return await query
                .OrderByDescending(l => l.ThoiGian)
                .Take(500)
                .Select(l => new AuditLogEntry
                {
                    Id = l.Id,
                    BaithiId = l.IdBaiThi,
                    ActionType = l.LoaiThaoTac,
                    Description = l.ChiTiet,
                    Timestamp = l.ThoiGian,
                    IpAddress = l.DiaChiIp,
                    UserAgent = l.UserAgent
                })
                .ToListAsync();
        }
    }
}