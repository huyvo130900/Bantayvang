using Microsoft.AspNetCore.SignalR;

namespace BanTayVang.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time exam monitoring
    /// - Supervisors can watch exam progress live
    /// - Anti-cheat warnings are pushed in real-time
    /// - Exam status changes are broadcast
    /// </summary>
    public class ExamMonitorHub : Hub
    {
        private readonly ILogger<ExamMonitorHub> _logger;

        public ExamMonitorHub(ILogger<ExamMonitorHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Supervisor joins monitoring room for a specific exam
        /// </summary>
        public async Task JoinExamMonitoring(int examId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-{examId}");
            _logger.LogInformation("Client {ConnectionId} joined monitoring for exam {ExamId}",
                Context.ConnectionId, examId);
        }

        /// <summary>
        /// Leave monitoring room
        /// </summary>
        public async Task LeaveExamMonitoring(int examId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exam-{examId}");
        }

        /// <summary>
        /// Student joins their exam session room
        /// </summary>
        public async Task JoinExamSession(int baithiId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{baithiId}");
        }

        /// <summary>
        /// Student sends heartbeat (proves they're still active)
        /// </summary>
        public async Task SendHeartbeat(int baithiId)
        {
            await Clients.Group($"exam-monitor-all").SendAsync("StudentHeartbeat", new
            {
                BaithiId = baithiId,
                Timestamp = DateTime.UtcNow,
                ConnectionId = Context.ConnectionId
            });
        }

        /// <summary>
        /// Join global monitoring (all exams)
        /// </summary>
        public async Task JoinGlobalMonitoring()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "exam-monitor-all");
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Service to push notifications through SignalR from anywhere in the app
    /// </summary>
    public interface IExamMonitorNotifier
    {
        Task NotifyCheatingWarning(int examId, int baithiId, string username, string warningType, string description);
        Task NotifyExamStarted(int examId, int userId, string username);
        Task NotifyExamSubmitted(int examId, int baithiId, string username, double score);
        Task NotifyExamStatusChanged(int examId, string newStatus);
        Task NotifyStudentProgress(int examId, int baithiId, int answeredCount, int totalCount);
    }

    public class ExamMonitorNotifier : IExamMonitorNotifier
    {
        private readonly IHubContext<ExamMonitorHub> _hubContext;

        public ExamMonitorNotifier(IHubContext<ExamMonitorHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyCheatingWarning(int examId, int baithiId, string username, string warningType, string description)
        {
            await _hubContext.Clients.Group($"exam-{examId}").SendAsync("CheatingWarning", new
            {
                ExamId = examId,
                BaithiId = baithiId,
                Username = username,
                WarningType = warningType,
                Description = description,
                Timestamp = DateTime.UtcNow
            });

            await _hubContext.Clients.Group("exam-monitor-all").SendAsync("CheatingWarning", new
            {
                ExamId = examId,
                BaithiId = baithiId,
                Username = username,
                WarningType = warningType,
                Description = description,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamStarted(int examId, int userId, string username)
        {
            await _hubContext.Clients.Group($"exam-{examId}").SendAsync("ExamStarted", new
            {
                ExamId = examId,
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamSubmitted(int examId, int baithiId, string username, double score)
        {
            await _hubContext.Clients.Group($"exam-{examId}").SendAsync("ExamSubmitted", new
            {
                ExamId = examId,
                BaithiId = baithiId,
                Username = username,
                Score = score,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamStatusChanged(int examId, string newStatus)
        {
            await _hubContext.Clients.Group($"exam-{examId}").SendAsync("ExamStatusChanged", new
            {
                ExamId = examId,
                NewStatus = newStatus,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyStudentProgress(int examId, int baithiId, int answeredCount, int totalCount)
        {
            await _hubContext.Clients.Group($"exam-{examId}").SendAsync("StudentProgress", new
            {
                ExamId = examId,
                BaithiId = baithiId,
                AnsweredCount = answeredCount,
                TotalCount = totalCount,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}