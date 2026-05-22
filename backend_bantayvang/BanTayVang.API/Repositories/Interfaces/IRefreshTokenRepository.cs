using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for refresh token operations
    /// Follows Interface Segregation Principle
    /// </summary>
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
    {
        /// <summary>
        /// Get refresh token by token value
        /// </summary>
        /// <param name="token">Token value</param>
        /// <returns>Refresh token if found</returns>
        Task<RefreshToken?> GetByTokenAsync(string token);

        /// <summary>
        /// Get all active refresh tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of active refresh tokens</returns>
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);

        /// <summary>
        /// Revoke all refresh tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of tokens revoked</returns>
        Task<int> RevokeAllUserTokensAsync(int userId);

        /// <summary>
        /// Revoke refresh token by token value
        /// </summary>
        /// <param name="token">Token value</param>
        /// <returns>True if revoked successfully</returns>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Clean up expired tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// Get refresh tokens by IP address for security monitoring
        /// </summary>
        /// <param name="ipAddress">IP address</param>
        /// <param name="hours">Hours to look back</param>
        /// <returns>List of refresh tokens</returns>
        Task<List<RefreshToken>> GetTokensByIpAddressAsync(string ipAddress, int hours = 24);
    }
}