using BanTayVang.API.DTOs.Common;
using System.Net;
using System.Text.Json;

namespace BanTayVang.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// OWASP A09: Security Logging - Centralized error logging
    /// OWASP A05: Don't expose internal errors in production
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogError(exception,
                "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId, context.Request.Path, context.Request.Method);

            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Không có quyền truy cập"),
                ArgumentNullException => (HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ"),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Không tìm thấy dữ liệu"),
                TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout"),
                _ => (HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new BaseResponseDto
            {
                Success = false,
                Message = message,
                Errors = _env.IsDevelopment()
                    ? new List<string> { exception.Message, exception.StackTrace ?? "", $"CorrelationId: {correlationId}" }
                    : new List<string> { $"CorrelationId: {correlationId}" }
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}