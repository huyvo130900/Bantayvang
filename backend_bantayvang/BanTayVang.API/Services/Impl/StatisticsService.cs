using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Statistics;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class StatisticsService : Services.Interfaces.IStatisticsService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(BanTayVangDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<DashboardDto>> GetDashboardAsync()
        {
            try
            {
                // Kỳ thi luyện tập (IsPracticeMode) tồn tại vĩnh viễn để học viên tự luyện - không
                // phải kỳ thi thật, nên đề thi/bài nộp thuộc các kỳ thi này phải loại khỏi mọi
                // thống kê chính thức bên dưới (tổng số đề, số bài nộp, điểm trung bình, hoạt động
                // gần đây), nếu không sẽ làm sai lệch báo cáo cho ban giám đốc/quản lý khoa.
                var practiceExamCampaignIds = await _context.ExamCampaigns
                    .Where(k => k.IsPracticeMode)
                    .Select(k => k.Id)
                    .ToListAsync();

                var examPapersQuery = _context.ExamPapers
                    .Where(d => d.ExamCampaignId == null || !practiceExamCampaignIds.Contains(d.ExamCampaignId.Value));
                var submissionsQuery = _context.ExamSubmissions
                    .Where(b => b.ExamCampaignId == null || !practiceExamCampaignIds.Contains(b.ExamCampaignId.Value));

                var dashboard = new DashboardDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.Status == true),
                    TotalQuestions = await _context.Questions.CountAsync(c => c.IsDeleted != true),
                    TotalExams = await examPapersQuery.CountAsync(),
                    ActiveExams = await examPapersQuery.CountAsync(d => d.Status == "Active"),
                    TotalSubmissions = await submissionsQuery.CountAsync(),
                    InProgressExams = await submissionsQuery.CountAsync(b => b.Status == "InProgress"),
                    CompletedExams = await submissionsQuery.CountAsync(b => b.Status == "Completed"),
                    TotalCheatingWarnings = await _context.CheatWarnings.CountAsync()
                };

                var completedScores = await submissionsQuery
                    .Where(b => b.Status == "Completed" && b.TotalScore != null)
                    .Select(b => (double)b.TotalScore.GetValueOrDefault())
                    .ToListAsync();

                dashboard.AverageScore = completedScores.Any() ? completedScores.Average() : 0;

                // Recent activities (last 10 completed exams)
                var recentExams = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.Status == "Completed" && (b.ExamCampaignId == null || !practiceExamCampaignIds.Contains(b.ExamCampaignId.Value)))
                    .OrderByDescending(b => b.SubmitTime)
                    .Take(10)
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .ToListAsync();

                dashboard.RecentActivities = recentExams.Select(b => new RecentActivityDto
                {
                    ActivityType = "EXAM_COMPLETED",
                    Description = $"Hoàn thành đề thi {b.ExamPaper?.ExamPaperCode} - Điểm: {b.TotalScore}",
                    Timestamp = b.SubmitTime ?? DateTime.UtcNow.AddHours(7),
                    Username = b.User?.Username
                }).ToList();

                return new BaseResponseDto<DashboardDto>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = dashboard
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard");
                return new BaseResponseDto<DashboardDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy dashboard",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<ExamStatisticsDto>> GetExamStatisticsAsync(int examCampaignId)
        {
            try
            {
                var examCampaign = await _context.ExamCampaigns.AsNoTracking().FirstOrDefaultAsync(k => k.Id == examCampaignId);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamStatisticsDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var submissions = await _context.ExamSubmissions
                    .AsNoTracking()
                    .Include(b => b.ExamPaper)
                    .Where(b => b.ExamCampaignId == examCampaignId)
                    .ToListAsync();

                // Group by participant to get unique candidate attempts
                var latestSubmissions = submissions
                    .Where(b => b.UserId != null)
                    .GroupBy(b => b.UserId!.Value)
                    .Select(g => g.OrderByDescending(b => b.Id).First())
                    .ToList();

                var latestCompletedSubmissions = submissions
                    .Where(b => b.UserId != null && b.Status == "Completed")
                    .GroupBy(b => b.UserId!.Value)
                    .Select(g => g.OrderByDescending(b => b.Id).First())
                    .ToList();

                // Normalize scores to a 10-point scale based on correct answers and total questions
                var scores = latestCompletedSubmissions
                    .Where(b => b.TotalScore.HasValue)
                    .Select(b => (double)b.TotalScore.GetValueOrDefault())
                    .ToList();

                var passCount = 0;
                var failCount = 0;
                foreach (var b in latestCompletedSubmissions)
                {
                    var threshold = examCampaign.MinPassQuestions ?? b.ExamPaper?.MinPassQuestions;
                    bool isPass;
                    if (threshold.HasValue)
                    {
                        isPass = (b.CorrectAnswers ?? 0) >= threshold.Value;
                    }
                    else
                    {
                        var totalQuestions = b.TotalQuestions ?? 10;
                        var defaultThreshold = totalQuestions > 0 ? (double)totalQuestions / 2 : 5;
                        isPass = (b.CorrectAnswers ?? 0) >= defaultThreshold;
                    }

                    if (isPass) passCount++;
                    else failCount++;
                }

                var stats = new ExamStatisticsDto
                {
                    ExamCampaignId = examCampaign.Id,
                    CampaignCode = examCampaign.CampaignCode,
                    CampaignName = examCampaign.CampaignName,
                    TotalParticipants = latestSubmissions.Count,
                    CompletedCount = latestCompletedSubmissions.Count,
                    InProgressCount = latestSubmissions.Count(s => !latestCompletedSubmissions.Any(c => c.UserId == s.UserId)),
                    AverageScore = scores.Any() ? scores.Average() : 0,
                    HighestScore = scores.Any() ? scores.Max() : 0,
                    LowestScore = scores.Any() ? scores.Min() : 0,
                    PassCount = passCount,
                    FailCount = failCount,
                    PassRate = latestCompletedSubmissions.Any() ? (double)passCount / latestCompletedSubmissions.Count * 100 : 0
                };

                // Score distribution
                var ranges = new[] { (0, 2), (2, 4), (4, 6), (6, 8), (8, 10) };
                stats.ScoreDistribution = ranges.Select(r => new ScoreDistributionDto
                {
                    Range = $"{r.Item1}-{r.Item2}",
                    Count = scores.Count(s => s >= r.Item1 && s < r.Item2),
                    Percentage = scores.Any() ? (double)scores.Count(s => s >= r.Item1 && s < r.Item2) / scores.Count * 100 : 0
                }).ToList();

                return new BaseResponseDto<ExamStatisticsDto>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam statistics");
                return new BaseResponseDto<ExamStatisticsDto>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thống kê",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<List<UserExamHistoryDto>>> GetUserExamHistoryAsync(int userId, bool applyPublishGate = false)
        {
            try
            {
                var history = await _context.ExamSubmissions
                    .AsNoTracking()
                    .Where(b => b.UserId == userId)
                    .Include(b => b.ExamPaper)
                    .OrderByDescending(b => b.StartTime ?? b.SubmitTime)
                    .Select(b => new
                    {
                        b.Id,
                        ExamPaperCode = b.ExamPaper!.ExamPaperCode,
                        ExamPaperName = b.ExamPaper.ExamPaperName,
                        b.StartTime,
                        b.SubmitTime,
                        b.Status,
                        b.CorrectAnswers,
                        b.TotalQuestions,
                        b.TotalScore,
                        b.WarningCount,
                        IsPublished = b.IsIndividualResultPublished || (b.ExamPaper != null && b.ExamPaper.IsResultPublished)
                    })
                    .ToListAsync();

                var result = history.Select(b => new UserExamHistoryDto
                {
                    ExamSubmissionId = b.Id,
                    ExamPaperCode = b.ExamPaperCode,
                    ExamPaperName = b.ExamPaperName,
                    StartTime = b.StartTime,
                    SubmitTime = b.SubmitTime,
                    Status = b.Status,
                    // BUG FIX: this method backs both the ManagementOnly admin/deptmanager view
                    // (applyPublishGate=false, allowed to see scores anytime) and the self-service
                    // "my-history" endpoint (applyPublishGate=true) - previously scores/correct-count
                    // leaked to the student immediately after auto-grading, before the department
                    // ever clicked "công bố điểm", bypassing the same publish-gate already enforced
                    // in ExamService.GetMyResultsAsync and GradingController.GetResultDetail.
                    CorrectAnswers = (!applyPublishGate || b.IsPublished) ? b.CorrectAnswers : null,
                    TotalQuestions = b.TotalQuestions,
                    TotalScore = (!applyPublishGate || b.IsPublished) ? b.TotalScore : null,
                    WarningCount = b.WarningCount
                }).ToList();

                return new BaseResponseDto<List<UserExamHistoryDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user history");
                return new BaseResponseDto<List<UserExamHistoryDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy lịch sử thi",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<List<TopPerformerDto>>> GetTopPerformersAsync(int top = 10)
        {
            try
            {
                // Loại bài nộp thuộc kỳ thi luyện tập (IsPracticeMode) khỏi bảng xếp hạng - đây
                // không phải kết quả thi thật.
                var performers = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.Status == "Completed" && b.TotalScore != null && b.UserId != null
                        && (b.ExamCampaign == null || !b.ExamCampaign.IsPracticeMode))
                    .Include(b => b.User)
                    .GroupBy(b => b.UserId)
                    .Select(g => new TopPerformerDto
                    {
                        UserId = g.Key!.Value,
                        Username = g.First().User!.Username,
                        FullName = g.First().User!.FullName,
                        Department = g.First().User!.Department,
                        ExamsTaken = g.Count(),
                        AverageScore = g.Average(b => b.TotalScore!.Value),
                        HighestScore = g.Max(b => b.TotalScore!.Value)
                    })
                    .OrderByDescending(p => p.AverageScore)
                    .Take(top)
                    .ToListAsync();

                return new BaseResponseDto<List<TopPerformerDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = performers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                return new BaseResponseDto<List<TopPerformerDto>>
                {
                    Success = false,
                    Message = "Lỗi khi lấy top performers",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}

