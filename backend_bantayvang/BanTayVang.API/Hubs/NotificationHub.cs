using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BanTayVang.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time general notifications
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.Request.Query["userId"].ToString();
            
            if (string.IsNullOrEmpty(userIdStr))
            {
                // Fallback to ClaimsPrincipal if authenticated
                userIdStr = Context.User?.FindFirst("user_id")?.Value 
                            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (int.TryParse(userIdStr, out var userId))
            {
                // Add this connection to the user's private group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                _logger.LogInformation("User {UserId} connected to NotificationHub (Connection: {ConnectionId})", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Anonymous client connected to NotificationHub (Connection: {ConnectionId})", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected from NotificationHub: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
