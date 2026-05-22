using BanTayVang.API.Configuration;
using BanTayVang.API.Models;
using BanTayVang.API.Models.Enums;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BanTayVang.API.Services.Impl.Auth
{
    /// <summary>
    /// JWT service implementation
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure token validation parameters
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };
        }

        public string GenerateAccessToken(Taikhoan user, bool rememberMe = false)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                // Determine token expiration
                var expirationMinutes = rememberMe 
                    ? _jwtSettings.RememberMeExpirationDays * 24 * 60 
                    : _jwtSettings.AccessTokenExpirationMinutes;

                var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

                // Create claims
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.TenDangNhap ?? string.Empty),
                    new(ClaimTypes.Email, user.Email ?? string.Empty),
                    new(ClaimTypes.GivenName, user.HoTen ?? string.Empty),
                    new(ClaimTypes.Role, GetUserRole(user.IdVaiTro).ToString()),
                    new("user_id", user.Id.ToString()),
                    new("username", user.TenDangNhap ?? string.Empty),
                    new("full_name", user.HoTen ?? string.Empty),
                    new("role_id", user.IdVaiTro?.ToString() ?? "0"),
                    new("is_active", user.TrangThai?.ToString() ?? "false"),
                    new("remember_me", rememberMe.ToString().ToLower()),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Create token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                // Generate token
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("Generated JWT token for user {UserId} with expiration {Expiration}", 
                    user.Id, expires);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to generate access token", ex);
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                // Generate cryptographically secure random bytes
                var randomBytes = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);
                
                return Convert.ToBase64String(randomBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw new InvalidOperationException("Failed to generate refresh token", ex);
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                // Validate token
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                // Ensure token is JWT and uses correct algorithm
                if (validatedToken is not JwtSecurityToken jwtToken || 
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid token algorithm or format");
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Token has expired");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating token");
                return null;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst("user_id")?.Value ?? 
                             principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public string? GetUsernameFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst("username")?.Value ?? 
                   principal?.FindFirst(ClaimTypes.Name)?.Value;
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                return jsonToken.ValidTo < DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking token expiration");
                return true; // Assume expired if we can't read it
            }
        }

        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                return jsonToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting token expiration");
                return null;
            }
        }

        public string GeneratePasswordResetToken(Taikhoan user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                // Short-lived token for password reset (15 minutes)
                var expires = DateTime.UtcNow.AddMinutes(15);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new("purpose", "password_reset"),
                    new("user_id", user.Id.ToString()),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to generate password reset token", ex);
            }
        }

        public bool ValidatePasswordResetToken(string token, int userId)
        {
            try
            {
                var principal = ValidateToken(token);
                if (principal == null)
                    return false;

                // Check purpose
                var purpose = principal.FindFirst("purpose")?.Value;
                if (purpose != "password_reset")
                    return false;

                // Check user ID
                var tokenUserId = principal.FindFirst("user_id")?.Value;
                if (!int.TryParse(tokenUserId, out var parsedUserId) || parsedUserId != userId)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating password reset token");
                return false;
            }
        }

        #region Private Helper Methods

        private static UserRole GetUserRole(int? roleId)
        {
            return roleId switch
            {
                1 => UserRole.Admin,
                2 => UserRole.Teacher,
                3 => UserRole.Student,
                4 => UserRole.Supervisor,
                _ => UserRole.Student // Default to student
            };
        }

        #endregion
    }
}