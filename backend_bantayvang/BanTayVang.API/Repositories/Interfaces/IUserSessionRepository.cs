using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for user session operations
    /// OWASP A09: Security Logging and Monitoring
    /// </summary>
    public interface IUserSessionRepository : IBaseRepository<UserSession>
    {
        /// <summary>
        /// Get session by session ID
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>User session if found</returns>
        Task<UserSession?> GetBySessionIdAsync(string sessionId);

        /// <summary>
        /// Get active sessions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of active sessions</returns>
        Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId);

        /// <summary>
        /// End session by session ID
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="endReason">Reason for ending session</param>
        /// <returns>True if ended successfully</returns>
        Task<bool> EndSessionAsync(string sessionId, string endReason);

        /// <summary>
        /// End all sessions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="endReason">Reason for ending sessions</param>
        /// <returns>Number of sessions ended</returns>
        Task<int> EndAllUserSessionsAsync(int userId, string endReason);

        /// <summary>
        /// Update session activity timestamp
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateLastActivityAsync(string sessionId);

        /// <summary>
        /// Clean up expired sessions
        /// </summary>
        /// <returns>Number of sessions cleaned up</returns>
        Task<int> CleanupExpiredSessionsAsync();

        /// <summary>
        /// Get sessions by IP address for security monitoring
        /// </summary>
        /// <param name="ipAddress">IP address</param>
        /// <param name="hours">Hours to look back</param>
        /// <returns>List of sessions</returns>
        Task<List<UserSession>> GetSessionsByIpAddressAsync(string ipAddress, int hours = 24);

        /// <summary>
        /// Get concurrent sessions count for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of active sessions</returns>
        Task<int> GetConcurrentSessionsCountAsync(int userId);
    }
}