using BanTayVang.API.Services.Interfaces.Exams;

namespace BanTayVang.API.BackgroundJobs
{
    /// <summary>
    /// Background service that auto-submits expired exam sessions
    /// Runs every 1 minute
    /// OWASP A04: Insecure Design - prevents users from holding sessions indefinitely
    /// </summary>
    public class AutoSubmitExpiredExamsJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoSubmitExpiredExamsJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public AutoSubmitExpiredExamsJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoSubmitExpiredExamsJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoSubmitExpiredExamsJob started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var submissionService = scope.ServiceProvider.GetRequiredService<IExamSubmissionService>();
                    
                    var result = await submissionService.AutoSubmitExpiredExamsAsync(stoppingToken);
                    
                    if (result.Success)
                    {
                        _logger.LogDebug("Auto-submit job completed: {Message}", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoSubmitExpiredExamsJob");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
            }

            _logger.LogInformation("AutoSubmitExpiredExamsJob stopped");
        }
    }
}