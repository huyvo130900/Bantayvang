using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Middleware
{
    /// <summary>
    /// Audit log middleware - automatically logs sensitive HTTP operations
    /// OWASP A09: Security Logging
    /// </summary>
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        // Only log these HTTP methods (write operations)
        private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "DELETE", "PATCH"
        };

        // Skip these paths (too noisy)
        private static readonly string[] SkipPaths =
        {
            "/api/exam/answer",       // would log every keystroke
            "/api/notification/",     // notification operations
            "/swagger",
            "/health",
            "/hubs"
        };

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            await _next(context);

            // Only log after request completes
            try
            {
                if (!ShouldLog(context))
                    return;

                var userId = context.Items["UserId"] as int?;
                var ipAddress = GetClientIp(context);
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var path = context.Request.Path.Value ?? "";
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode;

                var actionType = $"{method}_{path.TrimStart('/').Replace('/', '_').ToUpperInvariant()}";
                if (actionType.Length > 100)
                    actionType = actionType.Substring(0, 100);

                var description = $"{method} {path} -> {statusCode}";

                await auditLogService.LogActionAsync(
                    actionType,
                    description,
                    userId,
                    null,
                    ipAddress,
                    userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit log middleware");
            }
        }

        private static bool ShouldLog(HttpContext context)
        {
            if (!AuditedMethods.Contains(context.Request.Method))
                return false;

            var path = context.Request.Path.Value ?? "";
            if (SkipPaths.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}