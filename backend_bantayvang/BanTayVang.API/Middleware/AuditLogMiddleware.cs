using BanTayVang.API.Services.Interfaces;
using System.Security.Claims;

namespace BanTayVang.API.Middleware
{
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "DELETE", "PATCH", "GET"
        };

        private static readonly string[] SkipPaths =
        {
            "/api/exam/answer",
            "/swagger",
            "/health",
            "/hubs",
            "/api/auth/login"
        };

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            await _next(context);

            try
            {
                if (!ShouldLog(context)) return;

                // Extract user info from JWT claims
                var userId = GetUserId(context);
                var username = GetUsername(context);
                var department = context.User?.FindFirst("department_claim")?.Value;

                var ipAddress = GetClientIp(context);
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var path = context.Request.Path.Value ?? "";
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode;

                // ActionType: short label e.g. "POST /api/Question"
                var actionType = $"{method} {path}";
                if (actionType.Length > 100) actionType = actionType.Substring(0, 100);

                var description = $"{method} {path} → {statusCode}";

                await auditLogService.LogActionAsync(
                    actionType: actionType,
                    description: description,
                    userId: userId,
                    username: username,
                    examSubmissionId: null,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    method: method,
                    path: path,
                    statusCode: statusCode,
                    department: department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit log middleware");
            }
        }

        private static int? GetUserId(HttpContext context)
        {
            var val = context.User?.FindFirst("user_id")?.Value
                ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : null;
        }

        private static string? GetUsername(HttpContext context)
            => context.User?.FindFirst("username")?.Value
            ?? context.User?.FindFirst(ClaimTypes.Name)?.Value;

        private static bool ShouldLog(HttpContext context)
        {
            if (!AuditedMethods.Contains(context.Request.Method)) return false;
            var path = context.Request.Path.Value ?? "";
            return !SkipPaths.Any(s => path.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
