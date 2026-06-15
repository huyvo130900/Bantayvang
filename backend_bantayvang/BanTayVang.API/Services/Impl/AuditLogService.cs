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
            int? baithiId = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? method = null,
            string? path = null,
            int? statusCode = null,
            string? khoaPhong = null)
        {
            try
            {
                var log = new Logthaotac
                {
                    IdTaiKhoan = userId,
                    TenDangNhap = username,
                    LoaiThaoTac = actionType?.Length > 100 ? actionType.Substring(0, 100) : actionType,
                    ChiTiet = description?.Length > 4000 ? description.Substring(0, 4000) : description,
                    PhuongThuc = method,
                    DuongDan = path?.Length > 500 ? path.Substring(0, 500) : path,
                    MaHttp = statusCode,
                    ThoiGian = DateTime.Now,
                    IdBaiThi = baithiId,
                    DiaChiIp = ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                    KhoaPhong = khoaPhong
                };

                _context.Logthaotacs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log: {ActionType}", actionType);
            }
        }

        public async Task<List<AuditLogEntry>> GetRecentLogsAsync(int top = 500)
        {
            return await _context.Logthaotacs
                .Include(l => l.TaiKhoan)
                .OrderByDescending(l => l.ThoiGian)
                .Take(top)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetUserLogsAsync(int userId, int top = 100)
        {
            return await _context.Logthaotacs
                .Where(l => l.IdTaiKhoan == userId)
                .OrderByDescending(l => l.ThoiGian)
                .Take(top)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetExamSessionLogsAsync(int baithiId)
        {
            return await _context.Logthaotacs
                .Where(l => l.IdBaiThi == baithiId)
                .OrderByDescending(l => l.ThoiGian)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> SearchLogsAsync(
            string? actionType = null,
            string? username = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.Logthaotacs.AsQueryable();

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(l => l.LoaiThaoTac != null && l.LoaiThaoTac.Contains(actionType));

            if (!string.IsNullOrEmpty(username))
                query = query.Where(l => l.TenDangNhap != null && l.TenDangNhap.Contains(username));

            if (from.HasValue)
                query = query.Where(l => l.ThoiGian >= from);

            if (to.HasValue)
                query = query.Where(l => l.ThoiGian <= to.Value.AddDays(1));

            return await query
                .OrderByDescending(l => l.ThoiGian)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => MapToEntry(l))
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(
            string? actionType = null,
            string? username = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _context.Logthaotacs.AsQueryable();
            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(l => l.LoaiThaoTac != null && l.LoaiThaoTac.Contains(actionType));
            if (!string.IsNullOrEmpty(username))
                query = query.Where(l => l.TenDangNhap != null && l.TenDangNhap.Contains(username));
            if (from.HasValue) query = query.Where(l => l.ThoiGian >= from);
            if (to.HasValue) query = query.Where(l => l.ThoiGian <= to.Value.AddDays(1));
            return await query.CountAsync();
        }

        private static AuditLogEntry MapToEntry(Logthaotac l) => new()
        {
            Id = l.Id,
            UserId = l.IdTaiKhoan,
            Username = l.TenDangNhap ?? l.TaiKhoan?.TenDangNhap,
            BaithiId = l.IdBaiThi,
            ActionType = l.LoaiThaoTac,
            Method = l.PhuongThuc,
            Path = l.DuongDan,
            StatusCode = l.MaHttp,
            Description = l.ChiTiet,
            Timestamp = l.ThoiGian,
            IpAddress = l.DiaChiIp,
            UserAgent = l.UserAgent,
            KhoaPhong = l.KhoaPhong
        };
    }
}
