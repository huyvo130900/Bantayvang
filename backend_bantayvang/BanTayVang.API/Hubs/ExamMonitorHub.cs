using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BanTayVang.API.Helpers;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time exam monitoring
    /// - Supervisors can watch exam progress live
    /// - Anti-cheat warnings are pushed in real-time
    /// - Exam status changes are broadcast
    /// </summary>
    [Authorize]
    public class ExamMonitorHub : Hub
    {
        private readonly ILogger<ExamMonitorHub> _logger;
        private readonly IExamCampaignService _examCampaignService;
        private readonly IExamSubmissionRepository _examSubmissionRepository; // [FIX] để verify ownership

        public ExamMonitorHub(ILogger<ExamMonitorHub> logger, IExamCampaignService examCampaignService,
            IExamSubmissionRepository examSubmissionRepository)
        {
            _logger = logger;
            _examCampaignService = examCampaignService;
            _examSubmissionRepository = examSubmissionRepository;
        }

        /// <summary>
        /// Supervisor joins monitoring room for a specific exam campaign
        /// </summary>
        public async Task JoinCampaignMonitoring(int examCampaignId)
        {
            if (!DepartmentAuthHelper.IsAdmin(Context.User!) && !DepartmentAuthHelper.IsDeptManager(Context.User!))
                throw new HubException("Không có quyền giám sát kỳ thi.");

            // BUG FIX: `if (myDeptId.HasValue)` skipped this whole check for a DeptManager whose
            // managed_department_id claim is empty, letting them join monitoring for any campaign.
            // A campaign can now be scoped to 1-n departments (DepartmentIds) instead of one.
            if (DepartmentAuthHelper.IsDeptManager(Context.User!))
            {
                var myDeptId = DepartmentAuthHelper.GetDeptManagerDepartmentId(Context.User!);
                var campaign = await _examCampaignService.GetByIdAsync(examCampaignId);
                if (myDeptId == null || campaign.Data == null || !campaign.Data.DepartmentIds.Contains(myDeptId.Value))
                    throw new HubException("Kỳ thi này không thuộc khoa bạn quản lý.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"campaign-{examCampaignId}");
            _logger.LogInformation("Client {ConnectionId} joined monitoring for campaign {ExamCampaignId}",
                Context.ConnectionId, examCampaignId);
        }

        /// <summary>
        /// Leave monitoring room
        /// </summary>
        public async Task LeaveCampaignMonitoring(int examCampaignId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign-{examCampaignId}");
        }

        /// <summary>
        /// Student joins their exam session room
        /// </summary>
        public async Task JoinExamSession(int examSubmissionId)
        {
            // [FIX] Trước đây không check gì cả -> bất kỳ user nào đăng nhập cũng join được group
            // "session-{id}" của người khác, có thể nhận được ForceSubmitTriggered dành cho người khác
            // (rò rỉ tín hiệu, tuy không phải thông tin nhạy cảm nhưng vẫn là lỗi kiểm soát truy cập).
            var callerId = DepartmentAuthHelper.GetUserId(Context.User!);
            if (callerId == null)
                throw new HubException("Không xác định được người dùng.");

            var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
            if (examSubmission == null || examSubmission.UserId != callerId.Value)
                throw new HubException("Bạn không có quyền tham gia phiên thi này.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{examSubmissionId}");
        }

        /// <summary>
        /// Student sends heartbeat (proves they're still active)
        /// </summary>
        public async Task SendHeartbeat(int examSubmissionId)
        {
            await Clients.Group("exam-monitor-all").SendAsync("StudentHeartbeat", new
            {
                ExamSubmissionId = examSubmissionId,
                Timestamp = DateTime.UtcNow,
                ConnectionId = Context.ConnectionId
            });
        }

        /// <summary>
        /// Join global monitoring (all exams)
        /// </summary>
        public async Task JoinGlobalMonitoring()
        {
            if (!DepartmentAuthHelper.IsAdmin(Context.User!))
                throw new HubException("Chỉ Admin mới có quyền xem tổng quan tất cả kỳ thi.");
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
        Task NotifyCheatingWarning(int examCampaignId, int examSubmissionId, string username, string warningType, string description, int warningCount);
        Task NotifyExamStarted(int examCampaignId, int userId, string username);
        Task NotifyExamSubmitted(int examCampaignId, int examSubmissionId, string username, double score);
        Task NotifyExamStatusChanged(int examCampaignId, string newStatus);
        Task NotifyStudentProgress(int examCampaignId, int examSubmissionId, int answeredCount, int totalCount);
        Task NotifyForceSubmitTriggered(int examSubmissionId);
    }

    public class ExamMonitorNotifier : IExamMonitorNotifier
    {
        private readonly IHubContext<ExamMonitorHub> _hubContext;

        public ExamMonitorNotifier(IHubContext<ExamMonitorHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyCheatingWarning(int examCampaignId, int examSubmissionId, string username, string warningType, string description, int warningCount)
        {
            await _hubContext.Clients.Group($"campaign-{examCampaignId}").SendAsync("CheatingWarning", new
            {
                ExamCampaignId = examCampaignId,
                ExamSubmissionId = examSubmissionId,
                Username = username,
                WarningType = warningType,
                Description = description,
                WarningCount = warningCount,
                Timestamp = DateTime.UtcNow
            });

            await _hubContext.Clients.Group("exam-monitor-all").SendAsync("CheatingWarning", new
            {
                ExamCampaignId = examCampaignId,
                ExamSubmissionId = examSubmissionId,
                Username = username,
                WarningType = warningType,
                Description = description,
                WarningCount = warningCount,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamStarted(int examCampaignId, int userId, string username)
        {
            await _hubContext.Clients.Group($"campaign-{examCampaignId}").SendAsync("ExamStarted", new
            {
                ExamCampaignId = examCampaignId,
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamSubmitted(int examCampaignId, int examSubmissionId, string username, double score)
        {
            await _hubContext.Clients.Group($"campaign-{examCampaignId}").SendAsync("ExamSubmitted", new
            {
                ExamCampaignId = examCampaignId,
                ExamSubmissionId = examSubmissionId,
                Username = username,
                Score = score,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyExamStatusChanged(int examCampaignId, string newStatus)
        {
            await _hubContext.Clients.Group($"campaign-{examCampaignId}").SendAsync("ExamStatusChanged", new
            {
                ExamCampaignId = examCampaignId,
                NewStatus = newStatus,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyStudentProgress(int examCampaignId, int examSubmissionId, int answeredCount, int totalCount)
        {
            await _hubContext.Clients.Group($"campaign-{examCampaignId}").SendAsync("StudentProgress", new
            {
                ExamCampaignId = examCampaignId,
                ExamSubmissionId = examSubmissionId,
                AnsweredCount = answeredCount,
                TotalCount = totalCount,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyForceSubmitTriggered(int examSubmissionId)
        {
            await _hubContext.Clients.Group($"session-{examSubmissionId}")
                .SendAsync("ForceSubmitTriggered", new { ExamSubmissionId = examSubmissionId, Timestamp = DateTime.UtcNow });
        }
    }
}
