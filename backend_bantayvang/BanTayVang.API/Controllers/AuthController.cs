using BanTayVang.API.Attributes;
using BanTayVang.API.DTOs.Auth;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Authentication controller
    /// OWASP A01: Broken Access Control prevention
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// User registration
        /// </summary>
        /// <param name="registerDto">Registration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with JWT tokens</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in register endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra trong quá trình đăng ký" });
            }
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication response with JWT tokens</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Add client information to DTO
                loginDto.IpAddress = GetClientIpAddress();
                loginDto.UserAgent = Request.Headers.UserAgent.ToString();

                var result = await _authService.LoginAsync(loginDto, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra trong quá trình đăng nhập" });
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New authentication response</returns>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Add client information to DTO
                refreshTokenDto.IpAddress = GetClientIpAddress();

                var result = await _authService.RefreshTokenAsync(refreshTokenDto, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refresh token endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi làm mới token" });
            }
        }

        /// <summary>
        /// User logout
        /// </summary>
        /// <param name="logoutDto">Logout request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        [HttpPost("logout")]
        [RequireAuth]
        public async Task<IActionResult> Logout([FromBody] LogoutDto logoutDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _authService.LogoutAsync(logoutDto, userId, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in logout endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi đăng xuất" });
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="changePasswordDto">Change password request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        [HttpPost("change-password")]
        [RequireAuth]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _authService.ChangePasswordAsync(changePasswordDto, userId, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in change password endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu" });
            }
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = ExtractTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Token không được cung cấp" });
                }

                var result = await _authService.ValidateTokenAsync(token, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in validate token endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xác thực token" });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [RequireAuth]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in get current user endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy thông tin người dùng" });
            }
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _authService.RequestPasswordResetAsync(email, cancellationToken);
                
                // Always return success for security (don't reveal if email exists)
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request password reset endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi yêu cầu đặt lại mật khẩu" });
            }
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        /// <param name="resetRequest">Password reset request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest resetRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(resetRequest.Token, resetRequest.NewPassword, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reset password endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi đặt lại mật khẩu" });
            }
        }

        /// <summary>
        /// Revoke all user sessions (admin only)
        /// </summary>
        /// <param name="userId">User ID to revoke sessions for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        [HttpPost("revoke-sessions/{userId}")]
        [RequireRole(Models.Enums.UserRole.Admin)]
        public async Task<IActionResult> RevokeAllUserSessions(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _authService.RevokeAllUserSessionsAsync(userId, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in revoke sessions endpoint");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi thu hồi phiên đăng nhập" });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get current user ID from context
        /// </summary>
        private int GetCurrentUserId()
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User ID not found in context");
            }
            return userId.Value;
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Extract JWT token from Authorization header
        /// </summary>
        private string? ExtractTokenFromHeader()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return authHeader["Bearer ".Length..].Trim();
        }

        #endregion
    }

    /// <summary>
    /// Password reset request DTO
    /// </summary>
    public class PasswordResetRequest
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}