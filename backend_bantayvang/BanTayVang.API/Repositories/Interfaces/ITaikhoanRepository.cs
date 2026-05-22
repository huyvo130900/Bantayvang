using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for user account operations
    /// OWASP A01: Broken Access Control prevention
    /// </summary>
    public interface ITaikhoanRepository : IBaseRepository<Taikhoan>
    {
        /// <summary>
        /// Get user by username or email
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <returns>User account if found</returns>
        Task<Taikhoan?> GetByUsernameOrEmailAsync(string usernameOrEmail);

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User account if found</returns>
        Task<Taikhoan?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>User account if found</returns>
        Task<Taikhoan?> GetByEmailAsync(string email);

        /// <summary>
        /// Check if username exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <param name="excludeUserId">User ID to exclude from check (for updates)</param>
        /// <returns>True if username exists</returns>
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);

        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeUserId">User ID to exclude from check (for updates)</param>
        /// <returns>True if email exists</returns>
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);

        /// <summary>
        /// Get users by role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>List of users with the specified role</returns>
        Task<List<Taikhoan>> GetByRoleAsync(int roleId);

        /// <summary>
        /// Get active users
        /// </summary>
        /// <returns>List of active users</returns>
        Task<List<Taikhoan>> GetActiveUsersAsync();

        /// <summary>
        /// Update last login time
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="loginTime">Login timestamp</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateLastLoginAsync(int userId, DateTime loginTime);
    }
}