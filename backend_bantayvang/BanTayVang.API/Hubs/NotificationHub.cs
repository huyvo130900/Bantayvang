using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BanTayVang.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time general notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // OWASP A01: Broken Access Control - the group a connection joins MUST come
            // from the authenticated JWT (validated by [Authorize] + the "access_token"
            // query-string bridge already wired up in Program.cs for /hubs paths), never
            // from a client-supplied "userId" query parameter (that let anyone read anyone
            // else's private notifications by just changing the URL).
            var userIdStr = Context.User?.FindFirst("user_id")?.Value
                            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdStr, out var userId))
            {
                // Add this connection to the user's private group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                _logger.LogInformation("User {UserId} connected to NotificationHub (Connection: {ConnectionId})", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Authenticated client with unparsable user id connected to NotificationHub (Connection: {ConnectionId})", Context.ConnectionId);
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
