using BanTayVang.API.DTOs.Auth;
using BanTayVang.API.DTOs.Common;

namespace BanTayVang.API.Services.Interfaces.Auth
{
    /// <summary>
    /// Authentication service interface
    /// OWASP A01: Broken Access Control prevention
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with JWT tokens</returns>
        Task<BaseResponseDto<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="registerDto">Registration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with JWT tokens</returns>
        Task<BaseResponseDto<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New authentication response</returns>
        Task<BaseResponseDto<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logout user and invalidate tokens
        /// </summary>
        /// <param name="logoutDto">Logout request</param>
        /// <param name="userId">Current user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> LogoutAsync(LogoutDto logoutDto, int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="changePasswordDto">Change password request</param>
        /// <param name="userId">Current user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> ChangePasswordAsync(ChangePasswordDto changePasswordDto, int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with user info</returns>
        Task<BaseResponseDto<UserInfoDto>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current user information from token
        /// </summary>
        /// <param name="userId">User ID from token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User information</returns>
        Task<BaseResponseDto<UserInfoDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke all user sessions (logout from all devices)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> RevokeAllUserSessionsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate password reset token
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        /// <param name="token">Password reset token</param>
        /// <param name="newPassword">New password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    }
}