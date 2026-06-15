using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.BackgroundJobs
{
    /// <summary>
    /// Background job tự động công bố điểm khi đề thi hết hạn.
    /// Logic: Dethi.TrangThai = 'Active' VÀ ThoiGianBatDau + ThoiGianLamBai đã qua
    /// VÀ tất cả bài thi đã Completed/AutoSubmitted VÀ CongBoKetQua = false
    /// → tự động set CongBoKetQua = true.
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

            var now = DateTime.Now;

            // Lấy các Đề thi đang hoạt động có Kỳ thi liên kết đã hết hạn, chưa công bố
            var candidates = await db.Dethis
                .Include(d => d.KyThiNavigation)
                .Where(d => d.CongBoKetQua == false
                         && d.KyThiId != null
                         && d.KyThiNavigation != null
                         && d.KyThiNavigation.ThoiGianKetThuc != null
                         && d.KyThiNavigation.ThoiGianKetThuc <= now)
                .ToListAsync(ct);

            int published = 0;

            foreach (var dethi in candidates)
            {
                // Kiểm tra còn bài thi nào đang InProgress thuộc đề thi này không
                bool hasInProgress = await db.Baithis
                    .AnyAsync(b => b.IdDeThi == dethi.Id && b.TrangThai == "InProgress", ct);

                if (hasInProgress)
                    continue;

                // Công bố điểm cho đề thi này
                dethi.CongBoKetQua = true;
                dethi.ThoiGianCongBo = now;
                published++;

                _logger.LogInformation(
                    "Auto-published Dethi {TenDe} (Id={Id}) at {Time}",
                    dethi.TenDeThi, dethi.Id, now);
            }

            if (published > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("AutoPublish: published {Count} đề thi", published);
            }
        }
    }
}
