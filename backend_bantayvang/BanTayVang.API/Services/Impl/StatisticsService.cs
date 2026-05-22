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
                    .Where(b => b.TrangThai == "Completed" && b.TongDiem != null)
                    .Select(b => b.TongDiem!.Value)
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

        public async Task<BaseResponseDto<ExamStatisticsDto>> GetExamStatisticsAsync(int examId)
        {
            try
            {
                var exam = await _context.Dethis.FirstOrDefaultAsync(d => d.Id == examId);
                if (exam == null)
                    return new BaseResponseDto<ExamStatisticsDto> { Success = false, Message = "Không tìm thấy đề thi" };

                var submissions = await _context.Baithis
                    .Where(b => b.IdDeThi == examId)
                    .ToListAsync();

                var completed = submissions.Where(b => b.TrangThai == "Completed").ToList();
                var scores = completed.Where(b => b.TongDiem.HasValue).Select(b => b.TongDiem!.Value).ToList();

                var stats = new ExamStatisticsDto
                {
                    ExamId = exam.Id,
                    MaDeThi = exam.MaDeThi,
                    TenDeThi = exam.TenDeThi,
                    TotalParticipants = submissions.Count,
                    CompletedCount = completed.Count,
                    InProgressCount = submissions.Count(b => b.TrangThai == "InProgress"),
                    AverageScore = scores.Any() ? scores.Average() : 0,
                    HighestScore = scores.Any() ? scores.Max() : 0,
                    LowestScore = scores.Any() ? scores.Min() : 0,
                    PassCount = scores.Count(s => s >= 5),
                    FailCount = scores.Count(s => s < 5),
                    PassRate = scores.Any() ? (double)scores.Count(s => s >= 5) / scores.Count * 100 : 0
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