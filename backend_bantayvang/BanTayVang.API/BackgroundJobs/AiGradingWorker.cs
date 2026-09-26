using BanTayVang.API.Helpers;
using BanTayVang.API.Hubs;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Impl;
using BanTayVang.API.Services.Impl.Exams;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.BackgroundJobs
{
    /// <summary>
    /// Background Worker xu ly hang doi AI cham bai.
    /// Moi item trong queue la 1 SubmissionDetailId can cham.
    /// Pattern nay giong voi AutoPublishExpiredExamsJob.cs da co san.
    /// </summary>
    public class AiGradingWorker : BackgroundService
    {
        private readonly AiGradingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiGradingWorker> _logger;

        public AiGradingWorker(
            AiGradingQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<AiGradingWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AiGradingWorker started - cho doi cau tu luan can cham...");

            // BUG FIX (resilience): ProcessItemAsync already guards every item with its own
            // try/catch, but the `await foreach` enumeration itself had no equivalent guard -
            // unlike AutoPublishExpiredExamsJob, whose per-iteration try/catch keeps its polling
            // loop alive across transient errors. BackgroundService does not restart a faulted
            // ExecuteAsync, so any exception escaping the enumeration (not just per-item failures)
            // would permanently stop AI grading for the rest of the app's lifetime with no recovery.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var item in _queue.ReadAllAsync(stoppingToken))
                    {
                        await ProcessItemAsync(item.SubmissionDetailId, item.AutoFinalize, stoppingToken);
                    }
                    break; // channel completed normally (never happens today - never explicitly closed)
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break; // expected on app shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AiGradingWorker: loi khong mong doi trong vong lap doc queue, se thu lai sau 5s");
                    // BUG FIX: retried with no backoff at all - if the channel read fails
                    // immediately and persistently, this would spin the loop as fast as possible,
                    // pegging a CPU core and flooding the logs. Mirror AutoPublishExpiredExamsJob's
                    // delay-between-iterations pattern instead of retrying instantly forever.
                    try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("AiGradingWorker stopped.");
        }

        private async Task ProcessItemAsync(int submissionDetailId, bool autoFinalize, CancellationToken stoppingToken)
        {
            _logger.LogInformation("AI bat dau cham SubmissionDetail {Id}", submissionDetailId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BanTayVangDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIGradingService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                // Load detail kem theo du lieu can thiet
                var detail = await db.SubmissionDetails
                    .Include(c => c.Question)
                    .Include(c => c.ExamSubmission)
                        .ThenInclude(e => e!.User)
                    .FirstOrDefaultAsync(c => c.Id == submissionDetailId, stoppingToken);

                if (detail == null)
                {
                    _logger.LogWarning("Khong tim thay SubmissionDetail {Id}", submissionDetailId);
                    return;
                }

                // Tranh xu ly lai item da hoan thanh
                if (detail.AiGradingStatus == "Done")
                {
                    _logger.LogInformation("SubmissionDetail {Id} da duoc AI cham, bo qua", submissionDetailId);
                    return;
                }

                // Cap nhat trang thai → Processing
                detail.AiGradingStatus = "Processing";
                await db.SaveChangesAsync(stoppingToken);

                // Goi AI
                var (score, comment) = await aiService.GradeEssayAsync(
                    detail.Question?.Content ?? "",
                    detail.Question?.SuggestedAnswer,
                    detail.EssayAnswer,
                    detail.EssayImageUrl,
                    stoppingToken);

                // Luu ket qua AI vao cac cot rieng biet
                detail.AiScore = score;
                detail.AiComment = comment;
                detail.AiGradingStatus = "Done";

                // BUG FIX (race condition): `detail` was loaded into memory BEFORE the slow
                // aiService.GradeEssayAsync call above. If a teacher manually graded this same
                // SubmissionDetail (via a different request/DbContext) while we were waiting on the
                // AI response, that write never touched our in-memory copy - so checking
                // `detail.ScoreObtained == null` here would still see the stale pre-call value and
                // overwrite the teacher's just-entered score with the AI's. Use ExecuteUpdateAsync
                // scoped by `ScoreObtained == null` so the "is it still ungraded" check and the
                // write happen as one atomic DB operation against the current row, not our stale
                // snapshot. This update is deliberately separate from the SaveChangesAsync below,
                // which only ever touches AiScore/AiComment/AiGradingStatus on the tracked entity.
                //
                // autoFinalize=false ("AI gợi ý, người chấm duyệt lại" mode): skip this write entirely
                // so ScoreObtained stays null - AiScore/AiComment/AiGradingStatus="Done" above already
                // give the grader the AI's suggestion, and the existing review UI
                // (result-detail-dialog.tsx) requires them to pick 0/0.5/1 by hand before the score
                // counts as final (RecalculateTotalScoreAsync below only sums non-null ScoreObtained).
                if (autoFinalize)
                {
                    await db.SubmissionDetails
                        .Where(c => c.Id == submissionDetailId && c.ScoreObtained == null)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.ScoreObtained, score)
                            .SetProperty(c => c.TeacherComment, $"[AI] {comment}"), stoppingToken);
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("AI cham xong SubmissionDetail {Id}: score={Score}, autoFinalize={AutoFinalize}", submissionDetailId, score, autoFinalize);

                // Tinh lai TotalScore cua ExamSubmission
                if (detail.ExamSubmissionId.HasValue)
                {
                    await RecalculateTotalScoreAsync(db, detail.ExamSubmissionId.Value, stoppingToken);
                }

                // Doc lai ScoreObtained THAT SU vua duoc luu (khong gia dinh = score, vi
                // autoFinalize=false thi no van la null, va ke ca khi autoFinalize=true, mot giao
                // vien co the da cham tay truoc do khien ExecuteUpdateAsync o tren khong ghi de -
                // FE dua vao gia tri that nay thay vi tu doan de hien thi dung trang thai "da chot"
                // hay "chi la goi y AI".
                var actualScoreObtained = await db.SubmissionDetails
                    .Where(c => c.Id == submissionDetailId)
                    .Select(c => c.ScoreObtained)
                    .FirstOrDefaultAsync(stoppingToken);

                // Ban SignalR ve cho Giam khao dang xem man hinh
                // Dung group user-{userId} cua ExamSubmission owner - Giam khao dang online se nhan duoc
                var managerId = detail.ExamSubmission?.UserId ?? 0;
                try
                {
                    await hubContext.Clients
                        .All  // Thong bao cho tat ca management users dang online
                        .SendAsync("AiGradingDone", new
                        {
                            submissionDetailId = detail.Id,
                            examSubmissionId = detail.ExamSubmissionId,
                            aiScore = score,
                            aiComment = comment,
                            scoreObtained = actualScoreObtained
                        }, stoppingToken);
                }
                catch (Exception hubEx)
                {
                    // SignalR loi khong nen lam hong luong chinh
                    _logger.LogWarning(hubEx, "SignalR push that bai cho SubmissionDetail {Id}", submissionDetailId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi AI cham SubmissionDetail {Id}", submissionDetailId);

                // Danh dau Error de FE biet - KHONG dong ScoreObtained o day, de giao vien tu cham tay.
                // Luu ly do loi vao AiComment (khac voi TeacherComment/ScoreObtained) de admin xem duoc
                // nguyen nhan that su (vd sai model, sai API key, het quota...) ma khong anh huong diem.
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<BanTayVangDbContext>();
                    var detail = await db.SubmissionDetails.FindAsync(submissionDetailId);
                    if (detail != null)
                    {
                        detail.AiGradingStatus = "Error";
                        var errMsg = ex.Message ?? "Loi khong xac dinh";
                        detail.AiComment = errMsg.Length > 500 ? errMsg[..500] : errMsg;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Khong the cap nhat AiGradingStatus=Error cho {Id}", submissionDetailId);
                }
            }
        }

        /// <summary>
        /// Tinh lai TotalScore sau khi AI cham.
        /// Su dung ExamSubmissionService.CalculateTotalScore (ham dung chung toan he thong).
        /// </summary>
        private async Task RecalculateTotalScoreAsync(BanTayVangDbContext db, int examSubmissionId, CancellationToken ct)
        {
            var examSubmission = await db.ExamSubmissions
                .FirstOrDefaultAsync(b => b.Id == examSubmissionId, ct);

            if (examSubmission == null) return;

            // BUG FIX (confirmed live): this used to load SubmissionDetails through the
            // ExamSubmission's own .Include(), on the SAME DbContext ProcessItemAsync already used
            // to load `detail` (the very row just AI-graded) at the top of this method. EF Core's
            // identity map means a query for an entity that is already tracked in this context
            // returns the EXISTING in-memory instance instead of the fresh database row - so this
            // recalculation kept seeing `detail`'s STALE ScoreObtained (null, from before the
            // ExecuteUpdateAsync write above) instead of the score that was just persisted. The
            // essay's ScoreObtained was correctly saved to the database, but silently excluded from
            // the exam's CorrectAnswers/TotalScore every time - reproduced live: AI graded a essay
            // 1.0, the column showed 1.0, but the submission's total score never moved.
            // AsNoTracking forces a fresh read straight from the database, bypassing the stale
            // tracked instance entirely.
            var details = await db.SubmissionDetails
                .AsNoTracking()
                .Include(c => c.Question)
                    .ThenInclude(q => q!.QuestionCategory)
                .Include(c => c.Question)
                    .ThenInclude(q => q!.QuestionOptions)
                .Where(c => c.ExamSubmissionId == examSubmissionId)
                .ToListAsync(ct);

            double sumScore = 0;
            int correctAnswers = 0;

            var byQuestion = details
                .Where(c => c.QuestionId.HasValue)
                .GroupBy(c => c.QuestionId!.Value);

            foreach (var group in byQuestion)
            {
                var question = group.First().Question;
                var categoryName = question?.QuestionCategory?.CategoryName;
                bool isTuLuan = EssayQuestionHelper.IsEssay(question);

                if (isTuLuan)
                {
                    var s = group.First().ScoreObtained;
                    if (s > 0) correctAnswers++;
                    sumScore += s ?? 0;
                }
                else
                {
                    var groupSum = group.Sum(c => c.ScoreObtained ?? 0);
                    if (groupSum > 0) correctAnswers++;
                    sumScore += groupSum;
                }
            }

            examSubmission.CorrectAnswers = correctAnswers;
            examSubmission.TotalScore = ExamSubmissionService.CalculateTotalScore(sumScore, examSubmission.TotalQuestions ?? 0);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Tinh lai TotalScore ExamSubmission {Id}: {Score}", examSubmissionId, examSubmission.TotalScore);
        }
    }
}
