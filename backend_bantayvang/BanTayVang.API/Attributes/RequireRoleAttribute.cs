using BanTayVang.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BanTayVang.API.Attributes
{
    /// <summary>
    /// Attribute to require specific roles for controller actions
    /// OWASP A01: Broken Access Control prevention
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly UserRole[] _requiredRoles;

        public RequireRoleAttribute(params UserRole[] requiredRoles)
        {
            _requiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated first
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Yêu cầu đăng nhập để truy cập tài nguyên này",
                    statusCode = 401
                });
                return;
            }

            // Get user role from claims
            var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                context.Result = new ForbiddenObjectResult(new
                {
                    success = false,
                    message = "Không có thông tin vai trò người dùng",
                    statusCode = 403
                });
                return;
            }

            // Parse role
            if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
            {
                context.Result = new ForbiddenObjectResult(new
                {
                    success = false,
                    message = "Vai trò người dùng không hợp lệ",
                    statusCode = 403
                });
                return;
            }

            // Check if user has required role
            if (!_requiredRoles.Contains(userRole))
            {
                var requiredRoleNames = string.Join(", ", _requiredRoles.Select(r => r.ToString()));
                context.Result = new ForbiddenObjectResult(new
                {
                    success = false,
                    message = $"Yêu cầu vai trò: {requiredRoleNames}",
                    statusCode = 403
                });
                return;
            }
        }
    }

    /// <summary>
    /// Custom ForbiddenObjectResult for consistent error responses
    /// </summary>
    public class ForbiddenObjectResult : ObjectResult
    {
        public ForbiddenObjectResult(object value) : base(value)
        {
            StatusCode = 403;
        }
    }
}