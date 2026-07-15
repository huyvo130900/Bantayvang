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
                var dashboard = new DashboardDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.Status == true),
                    TotalQuestions = await _context.Questions.CountAsync(c => c.IsDeleted != true),
                    TotalExams = await _context.ExamPapers.CountAsync(),
                    ActiveExams = await _context.ExamPapers.CountAsync(d => d.Status == "Active"),
                    TotalSubmissions = await _context.ExamSubmissions.CountAsync(),
                    InProgressExams = await _context.ExamSubmissions.CountAsync(b => b.Status == "InProgress"),
                    CompletedExams = await _context.ExamSubmissions.CountAsync(b => b.Status == "Completed"),
                    TotalCheatingWarnings = await _context.CheatWarnings.CountAsync()
                };

                var completedScores = await _context.ExamSubmissions
                    .Where(b => b.Status == "Completed" && b.TotalScore != null && b.TotalQuestions != null && b.TotalQuestions > 0)
                    .Select(b => (double)b.TotalScore.GetValueOrDefault() / b.TotalQuestions.GetValueOrDefault() * 10)
                    .ToListAsync();

                dashboard.AverageScore = completedScores.Any() ? completedScores.Average() : 0;

                // Recent activities (last 10 completed exams)
                var recentExams = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Where(b => b.Status == "Completed")
                    .OrderByDescending(b => b.SubmitTime)
                    .Take(10)
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .ToListAsync();

                dashboard.RecentActivities = recentExams.Select(b => new RecentActivityDto
                {
                    ActivityType = "EXAM_COMPLETED",
                    Description = $"Hoàn thành đề thi {b.ExamPaper?.ExamPaperCode} - Điểm: {b.TotalScore}",
                    Timestamp = b.SubmitTime ?? DateTime.Now,
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
                var examCampaign = await _context.ExamCampaigns.FirstOrDefaultAsync(k => k.Id == examCampaignId);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamStatisticsDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var submissions = await _context.ExamSubmissions
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
                    .Where(b => b.TotalScore.HasValue && b.TotalQuestions.HasValue && b.TotalQuestions.Value > 0)
                    .Select(b => (double)b.TotalScore.GetValueOrDefault() / b.TotalQuestions.GetValueOrDefault() * 10)
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

        public async Task<BaseResponseDto<List<UserExamHistoryDto>>> GetUserExamHistoryAsync(int userId)
        {
            try
            {
                var history = await _context.ExamSubmissions
                    .Where(b => b.UserId == userId)
                    .Include(b => b.ExamPaper)
                    .OrderByDescending(b => b.StartTime ?? b.SubmitTime)
                    .Select(b => new UserExamHistoryDto
                    {
                        ExamSubmissionId = b.Id,
                        ExamPaperCode = b.ExamPaper!.ExamPaperCode,
                        ExamPaperName = b.ExamPaper.ExamPaperName,
                        StartTime = b.StartTime,
                        SubmitTime = b.SubmitTime,
                        Status = b.Status,
                        CorrectAnswers = b.CorrectAnswers,
                        TotalQuestions = b.TotalQuestions,
                        TotalScore = b.TotalScore,
                        SoCanhBao = b.TongSoCanhBao
                    })
                    .ToListAsync();

                return new BaseResponseDto<List<UserExamHistoryDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = history
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
                var performers = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Where(b => b.Status == "Completed" && b.TotalScore != null && b.UserId != null)
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