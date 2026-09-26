using BanTayVang.API.Services.Interfaces.Auth;
using System.Security.Claims;

namespace BanTayVang.API.Middleware
{
    /// <summary>
    /// JWT Authentication Middleware
    /// OWASP A01: Broken Access Control prevention
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            try
            {
                // Skip authentication for certain paths
                if (ShouldSkipAuthentication(context.Request.Path))
                {
                    await _next(context);
                    return;
                }

                // Extract token from Authorization header
                var token = ExtractTokenFromHeader(context.Request);
                if (string.IsNullOrEmpty(token))
                {
                    // No token provided - let the controller handle authorization
                    await _next(context);
                    return;
                }

                // Validate token
                var principal = jwtService.ValidateToken(token);
                if (principal == null)
                {
                    _logger.LogWarning("SECURITY: Invalid JWT token from {IpAddress}", GetClientIpAddress(context));
                    await _next(context);
                    return;
                }

                // BUG FIX (OWASP A01/A07): special-purpose tokens (currently only password-reset
                // tokens, marked with a "purpose" claim) must never be usable as a normal bearer
                // token here. Without this check, a password-reset token - obtainable via a
                // "forgot password" email and valid for 15 minutes - could be sent as a normal
                // Authorization header and would authenticate the caller for any [RequireAuth]
                // endpoint, even though it was only ever meant to be consumed by the dedicated
                // reset-password flow (which reads it from the request body, not this header, and
                // whose path is already in ShouldSkipAuthentication above - so this check does not
                // affect that flow at all).
                if (principal.FindFirst("purpose")?.Value is string purpose && !string.IsNullOrEmpty(purpose))
                {
                    _logger.LogWarning("SECURITY: Special-purpose token (purpose={Purpose}) rejected for general API access from {IpAddress}",
                        purpose, GetClientIpAddress(context));
                    await _next(context);
                    return;
                }

                // Set user context
                context.User = principal;

                // Extract user information for logging
                var userId = GetUserIdFromPrincipal(principal);
                var username = GetUsernameFromPrincipal(principal);

                // Add user information to context items for easy access
                context.Items["UserId"] = userId;
                context.Items["Username"] = username;
                context.Items["UserPrincipal"] = principal;

                // Log successful authentication (only for sensitive operations)
                if (IsSensitiveOperation(context.Request))
                {
                    _logger.LogInformation("SECURITY: User {UserId} authenticated for {Method} {Path}", 
                        userId, context.Request.Method, context.Request.Path);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JWT authentication middleware");
                // Continue to next middleware even if there's an error
                await _next(context);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Check if authentication should be skipped for this path
        /// </summary>
        private static bool ShouldSkipAuthentication(PathString path)
        {
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/refresh",
                "/api/auth/reset-password",
                "/api/auth/request-reset",
                "/api/auth/send-verification-code",
                "/api/auth/verify-email-code",
                "/api/auth/verify-reset-code",
                "/api/seed",
                "/swagger",
                "/health",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Extract JWT token from Authorization header or Query string (SignalR support)
        /// </summary>
        private static string? ExtractTokenFromHeader(HttpRequest request)
        {
            var authHeader = request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }

            // Also check query string (used by SignalR WebSockets)
            if (request.Query.TryGetValue("access_token", out var tokenValue))
            {
                return tokenValue.ToString();
            }

            return null;
        }

        /// <summary>
        /// Get client IP address from request
        /// </summary>
        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Extract user ID from claims principal
        /// </summary>
        private static int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst("user_id")?.Value ?? 
                             principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Extract username from claims principal
        /// </summary>
        private static string? GetUsernameFromPrincipal(ClaimsPrincipal principal)
        {
            return principal.FindFirst("username")?.Value ?? 
                   principal.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// Check if the current operation is sensitive and should be logged
        /// </summary>
        private static bool IsSensitiveOperation(HttpRequest request)
        {
            var sensitivePaths = new[]
            {
                "/api/exam",
                "/api/question",
                "/api/auth/change-password",
                "/api/auth/logout"
            };

            var sensitiveOperations = new[] { "POST", "PUT", "DELETE" };

            return sensitivePaths.Any(path => request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)) ||
                   sensitiveOperations.Contains(request.Method.ToUpperInvariant());
        }

        #endregion
    }
}