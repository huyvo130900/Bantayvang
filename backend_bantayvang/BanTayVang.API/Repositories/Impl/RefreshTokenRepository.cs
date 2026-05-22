using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    /// <summary>
    /// Refresh token repository implementation
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            return await _dbSet
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(rt => rt.UserId == userId && 
                           !rt.IsRevoked && 
                           !rt.IsUsed && 
                           rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await _dbSet
                .Where(rt => rt.UserId == userId && 
                           !rt.IsRevoked && 
                           rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
            return tokens.Count;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var refreshToken = await _dbSet
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null)
                return false;

            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _dbSet
                .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsUsed || rt.IsRevoked)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _dbSet.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }

            return expiredTokens.Count;
        }

        public async Task<List<RefreshToken>> GetTokensByIpAddressAsync(string ipAddress, int hours = 24)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return new List<RefreshToken>();

            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            return await _dbSet
                .Include(rt => rt.User)
                .Where(rt => rt.IpAddress == ipAddress && rt.CreatedAt >= cutoffTime)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
    }
}