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
                    TotalUsers = await _context.Taikhoans.CountAsync(),
                    ActiveUsers = await _context.Taikhoans.CountAsync(u => u.TrangThai == true),
                    TotalQuestions = await _context.Cauhois.CountAsync(c => c.DaXoa != true),
                    TotalExams = await _context.Dethis.CountAsync(),
                    ActiveExams = await _context.Dethis.CountAsync(d => d.TrangThai == "Active"),
                    TotalSubmissions = await _context.Baithis.CountAsync(),
                    InProgressExams = await _context.Baithis.CountAsync(b => b.TrangThai == "InProgress"),
                    CompletedExams = await _context.Baithis.CountAsync(b => b.TrangThai == "Completed"),
                    TotalCheatingWarnings = await _context.Canhbaogianlans.CountAsync()
                };

                var completedScores = await _context.Baithis
                    .Where(b => b.TrangThai == "Completed" && b.TongDiem != null && b.TongSoCau != null && b.TongSoCau > 0)
                    .Select(b => (double)b.TongDiem.GetValueOrDefault() / b.TongSoCau.GetValueOrDefault() * 10)
                    .ToListAsync();

                dashboard.AverageScore = completedScores.Any() ? completedScores.Average() : 0;

                // Recent activities (last 10 completed exams)
                var recentExams = await _context.Baithis
                    .Where(b => b.TrangThai == "Completed")
                    .OrderByDescending(b => b.ThoiGianNop)
                    .Take(10)
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .ToListAsync();

                dashboard.RecentActivities = recentExams.Select(b => new RecentActivityDto
                {
                    ActivityType = "EXAM_COMPLETED",
                    Description = $"Hoàn thành đề thi {b.IdDeThiNavigation?.MaDeThi} - Điểm: {b.TongDiem}",
                    Timestamp = b.ThoiGianNop ?? DateTime.Now,
                    Username = b.IdTaiKhoanNavigation?.TenDangNhap
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

        public async Task<BaseResponseDto<ExamStatisticsDto>> GetExamStatisticsAsync(int kyThiId)
        {
            try
            {
                var kyThi = await _context.KyThis.FirstOrDefaultAsync(k => k.Id == kyThiId);
                if (kyThi == null)
                    return new BaseResponseDto<ExamStatisticsDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var submissions = await _context.Baithis
                    .Include(b => b.IdDeThiNavigation)
                    .Where(b => b.IdKyThi == kyThiId)
                    .ToListAsync();

                // Group by participant to get unique candidate attempts
                var latestSubmissions = submissions
                    .Where(b => b.IdTaiKhoan != null)
                    .GroupBy(b => b.IdTaiKhoan!.Value)
                    .Select(g => g.OrderByDescending(b => b.Id).First())
                    .ToList();

                var latestCompletedSubmissions = submissions
                    .Where(b => b.IdTaiKhoan != null && b.TrangThai == "Completed")
                    .GroupBy(b => b.IdTaiKhoan!.Value)
                    .Select(g => g.OrderByDescending(b => b.Id).First())
                    .ToList();

                // Normalize scores to a 10-point scale based on correct answers and total questions
                var scores = latestCompletedSubmissions
                    .Where(b => b.TongDiem.HasValue && b.TongSoCau.HasValue && b.TongSoCau.Value > 0)
                    .Select(b => (double)b.TongDiem.GetValueOrDefault() / b.TongSoCau.GetValueOrDefault() * 10)
                    .ToList();

                var passCount = 0;
                var failCount = 0;
                foreach (var b in latestCompletedSubmissions)
                {
                    var threshold = kyThi.SoCauDungToiThieu ?? b.IdDeThiNavigation?.SoCauDungToiThieu;
                    bool isPass;
                    if (threshold.HasValue)
                    {
                        isPass = (b.SoCauDung ?? 0) >= threshold.Value;
                    }
                    else
                    {
                        var totalQuestions = b.TongSoCau ?? 10;
                        var defaultThreshold = totalQuestions > 0 ? (double)totalQuestions / 2 : 5;
                        isPass = (b.SoCauDung ?? 0) >= defaultThreshold;
                    }

                    if (isPass) passCount++;
                    else failCount++;
                }

                var stats = new ExamStatisticsDto
                {
                    KyThiId = kyThi.Id,
                    MaKyThi = kyThi.MaKyThi,
                    TenKyThi = kyThi.TenKyThi,
                    TotalParticipants = latestSubmissions.Count,
                    CompletedCount = latestCompletedSubmissions.Count,
                    InProgressCount = latestSubmissions.Count(s => !latestCompletedSubmissions.Any(c => c.IdTaiKhoan == s.IdTaiKhoan)),
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
                var history = await _context.Baithis
                    .Where(b => b.IdTaiKhoan == userId)
                    .Include(b => b.IdDeThiNavigation)
                    .OrderByDescending(b => b.ThoiGianBatDau ?? b.ThoiGianNop)
                    .Select(b => new UserExamHistoryDto
                    {
                        BaiThiId = b.Id,
                        MaDeThi = b.IdDeThiNavigation!.MaDeThi,
                        TenDeThi = b.IdDeThiNavigation.TenDeThi,
                        ThoiGianBatDau = b.ThoiGianBatDau,
                        ThoiGianNop = b.ThoiGianNop,
                        TrangThai = b.TrangThai,
                        SoCauDung = b.SoCauDung,
                        TongSoCau = b.TongSoCau,
                        TongDiem = b.TongDiem,
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
                var performers = await _context.Baithis
                    .Where(b => b.TrangThai == "Completed" && b.TongDiem != null && b.IdTaiKhoan != null)
                    .Include(b => b.IdTaiKhoanNavigation)
                    .GroupBy(b => b.IdTaiKhoan)
                    .Select(g => new TopPerformerDto
                    {
                        UserId = g.Key!.Value,
                        Username = g.First().IdTaiKhoanNavigation!.TenDangNhap,
                        FullName = g.First().IdTaiKhoanNavigation!.HoTen,
                        KhoaPhong = g.First().IdTaiKhoanNavigation!.KhoaPhong,
                        ExamsTaken = g.Count(),
                        AverageScore = g.Average(b => b.TongDiem!.Value),
                        HighestScore = g.Max(b => b.TongDiem!.Value)
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