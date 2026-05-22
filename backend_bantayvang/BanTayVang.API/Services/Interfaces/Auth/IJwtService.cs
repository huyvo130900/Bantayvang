using BanTayVang.API.Models;
using System.Security.Claims;

namespace BanTayVang.API.Services.Interfaces.Auth
{
    /// <summary>
    /// Service for JWT token generation and validation
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generate JWT access token for user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="rememberMe">Whether to extend token lifetime</param>
        /// <returns>JWT token string</returns>
        string GenerateAccessToken(Taikhoan user, bool rememberMe = false);

        /// <summary>
        /// Generate refresh token
        /// </summary>
        /// <returns>Refresh token string</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Validate JWT token and extract claims
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>Claims principal if valid, null otherwise</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Extract user ID from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User ID if valid, null otherwise</returns>
        int? GetUserIdFromToken(string token);

        /// <summary>
        /// Extract username from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Username if valid, null otherwise</returns>
        string? GetUsernameFromToken(string token);

        /// <summary>
        /// Check if token is expired
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>True if expired</returns>
        bool IsTokenExpired(string token);

        /// <summary>
        /// Get token expiration time
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Expiration datetime if valid</returns>
        DateTime? GetTokenExpiration(string token);

        /// <summary>
        /// Generate token for password reset
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>Password reset token</returns>
        string GeneratePasswordResetToken(Taikhoan user);

        /// <summary>
        /// Validate password reset token
        /// </summary>
        /// <param name="token">Password reset token</param>
        /// <param name="userId">Expected user ID</param>
        /// <returns>True if valid</returns>
        bool ValidatePasswordResetToken(string token, int userId);
    }
}