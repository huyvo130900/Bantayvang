using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    /// <summary>
    /// User session repository implementation
    /// OWASP A09: Security Logging and Monitoring
    /// </summary>
    public class UserSessionRepository : BaseRepository<UserSession>, IUserSessionRepository
    {
        public UserSessionRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<UserSession?> GetBySessionIdAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;

            return await _dbSet
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && 
                           s.IsActive && 
                           s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> EndSessionAsync(string sessionId, string endReason)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            var session = await _dbSet
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return false;

            session.IsActive = false;
            session.EndReason = endReason;
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> EndAllUserSessionsAsync(int userId, string endReason)
        {
            var sessions = await _dbSet
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.EndReason = endReason;
                session.LastActivityAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return sessions.Count;
        }

        public async Task<bool> UpdateLastActivityAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            var session = await _dbSet
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session == null)
                return false;

            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _dbSet
                .Where(s => s.ExpiresAt <= DateTime.UtcNow || !s.IsActive)
                .ToListAsync();

            if (expiredSessions.Any())
            {
                // Mark as inactive instead of deleting for audit purposes
                foreach (var session in expiredSessions.Where(s => s.IsActive))
                {
                    session.IsActive = false;
                    session.EndReason = "Expired";
                    session.LastActivityAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return expiredSessions.Count;
        }

        public async Task<List<UserSession>> GetSessionsByIpAddressAsync(string ipAddress, int hours = 24)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return new List<UserSession>();

            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            return await _dbSet
                .Include(s => s.User)
                .Where(s => s.IpAddress == ipAddress && s.CreatedAt >= cutoffTime)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetConcurrentSessionsCountAsync(int userId)
        {
            return await _dbSet
                .CountAsync(s => s.UserId == userId && 
                               s.IsActive && 
                               s.ExpiresAt > DateTime.UtcNow);
        }
    }
}