using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.BackgroundJobs
{
    /// <summary>
    /// Background job tự động công bố điểm khi đề thi hết hạn.
    /// Logic: ExamPaper.Status = 'Active' VÀ StartTime + DurationMinutes đã qua
    /// VÀ tất cả bài thi đã Completed/AutoSubmitted VÀ IsResultPublished = false
    /// → tự động set IsResultPublished = true.
    /// Chạy mỗi 2 phút.
    /// </summary>
    public class AutoPublishExpiredExamsJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoPublishExpiredExamsJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

        public AutoPublishExpiredExamsJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoPublishExpiredExamsJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoPublishExpiredExamsJob started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishExpiredExamsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoPublishExpiredExamsJob");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("AutoPublishExpiredExamsJob stopped");
        }

        private async Task PublishExpiredExamsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BanTayVangDbContext>();

            // `now` (fake-VN convention) is kept for stamping PublishedAt below, to stay consistent
            // with how PublishedAt/IndividualPublishedAt are set elsewhere (DepartmentController.cs,
            // GradingController.cs). ExamCampaign.EndTime, however, is stored as TRUE UTC (set via the
            // frontend's .toISOString()), so the expiry comparison below must use real UTC "now" -
            // comparing it against the fake-VN `now` made this job fire ~7 hours late/early.
            var now = DateTime.UtcNow.AddHours(7);
            var nowUtc = DateTime.UtcNow;

            // Lấy các Đề thi đang hoạt động có Kỳ thi liên kết đã hết hạn, chưa công bố
            var candidates = await db.ExamPapers
                .Include(d => d.ExamCampaign)
                .Where(d => d.IsResultPublished == false
                         && d.ExamCampaignId != null
                         && d.ExamCampaign != null
                         && d.ExamCampaign.EndTime != null
                         && d.ExamCampaign.EndTime <= nowUtc)
                .ToListAsync(ct);

            int published = 0;

            foreach (var examPaper in candidates)
            {
                // Kiểm tra còn bài thi nào đang InProgress thuộc đề thi này không
                bool hasInProgress = await db.ExamSubmissions
                    .AnyAsync(b => b.ExamPaperId == examPaper.Id && b.Status == "InProgress", ct);

                if (hasInProgress)
                    continue;

                // BUG FIX: DepartmentController.ToggleExamVisibility (manual publish) blocks
                // publishing while any essay question is still ungraded, but this auto-publish job
                // had no such check - it could reveal scores that don't yet include ungraded essay
                // answers. Apply the same guard so the two publish paths agree.
                bool hasUngradedEssay = await db.SubmissionDetails
                    .AnyAsync(c => c.ExamSubmission != null
                                && c.ExamSubmission.ExamPaperId == examPaper.Id
                                && c.ExamSubmission.Status == "Completed"
                                && c.Question != null
                                && c.Question.QuestionCategory != null
                                && EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName)
                                && c.ScoreObtained == null, ct);

                if (hasUngradedEssay)
                {
                    _logger.LogInformation(
                        "AutoPublish: skipping ExamPaper {TenDe} (Id={Id}) - still has ungraded essay answers",
                        examPaper.ExamPaperName, examPaper.Id);
                    continue;
                }

                // Công bố điểm cho đề thi này
                examPaper.IsResultPublished = true;
                examPaper.PublishedAt = now;
                published++;

                _logger.LogInformation(
                    "Auto-published ExamPaper {TenDe} (Id={Id}) at {Time}",
                    examPaper.ExamPaperName, examPaper.Id, now);
            }

            if (published > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("AutoPublish: published {Count} đề thi", published);
            }
        }
    }
}
