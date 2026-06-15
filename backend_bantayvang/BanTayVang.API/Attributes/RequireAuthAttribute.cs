using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanTayVang.API.Attributes
{
    /// <summary>
    /// Attribute to require authentication for controller actions
    /// OWASP A01: Broken Access Control prevention
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAuthAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
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

            // Check if user ID is available in context
            var userId = context.HttpContext.Items["UserId"] as int?;
            if (!userId.HasValue)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Thông tin xác thực không hợp lệ",
                    statusCode = 401
                });
                return;
            }
        }
    }
}