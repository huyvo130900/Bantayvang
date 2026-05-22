using BanTayVang.API.DTOs.AntiCheat;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Security;

namespace BanTayVang.API.Services.Impl.Security
{
    public class ExamSecurityService : IExamSecurityService
    {
        private readonly ICanhbaogianlanRepository _canhbaoRepository;
        private readonly IBaithiRepository _baithiRepository;
        private readonly ILogger<ExamSecurityService> _logger;
        private readonly IConfiguration _configuration;

        public ExamSecurityService(
            ICanhbaogianlanRepository canhbaoRepository,
            IBaithiRepository baithiRepository,
            ILogger<ExamSecurityService> logger,
            IConfiguration configuration)
        {
            _canhbaoRepository = canhbaoRepository;
            _baithiRepository = baithiRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<BaseResponseDto> LogSecurityEventAsync(string eventType, string description, int? userId = null, string severity = "Info", CancellationToken cancellationToken = default)
        {
            try
            {
                var logData = new
                {
                    EventType = eventType,
                    Description = description,
                    UserId = userId,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                switch (severity.ToLower())
                {
                    case "critical":
                    case "high":
                        _logger.LogError("SECURITY_EVENT: {EventType} - {Description} - User: {UserId} - Severity: {Severity} - CorrelationId: {CorrelationId}", 
                            eventType, description, userId, severity, logData.CorrelationId);
                        break;
                    case "medium":
                        _logger.LogWarning("SECURITY_EVENT: {EventType} - {Description} - User: {UserId} - Severity: {Severity} - CorrelationId: {CorrelationId}", 
                            eventType, description, userId, severity, logData.CorrelationId);
                        break;
                    default:
                        _logger.LogInformation("SECURITY_EVENT: {EventType} - {Description} - User: {UserId} - Severity: {Severity} - CorrelationId: {CorrelationId}", 
                            eventType, description, userId, severity, logData.CorrelationId);
                        break;
                }

                return BaseResponseDto.SuccessResult("Security event logged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event: {EventType}", eventType);
                return BaseResponseDto.FailureResult("Failed to log security event");
            }
        }

        public async Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa, CancellationToken cancellationToken = default)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                
                var canhbao = new Canhbaogianlan
                {
                    IdBaiThi = baithiId,
                    LoaiCanhBao = loaiCanhBao,
                    MoTa = moTa,
                    ThoiGian = DateTime.Now,
                    SoLanViPham = 1,
                    MucDoNghiemTrong = "Medium",
                    CorrelationId = correlationId
                };

                await _canhbaoRepository.AddAsync(canhbao);

                _logger.LogWarning("Suspicious activity logged: {Type} for exam session {ExamSessionId}. CorrelationId: {CorrelationId}", 
                    loaiCanhBao, baithiId, correlationId);

                return BaseResponseDto.SuccessResult("Suspicious activity logged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging suspicious activity for exam session {ExamSessionId}", baithiId);
                return BaseResponseDto.FailureResult("Failed to log suspicious activity");
            }
        }

        public async Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId, CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await _canhbaoRepository.GetCountByBaithiIdAsync(baithiId);
                return BaseResponseDto<int>.SuccessResult(count, "Warning count retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warning count for exam session {ExamSessionId}", baithiId);
                return BaseResponseDto<int>.FailureResult("Failed to get warning count");
            }
        }

        public async Task<BaseResponseDto<bool>> ValidateExamEnvironmentAsync(Dictionary<string, object> environmentData, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple validation logic
                var isValid = true;
                
                // Add validation logic here
                
                return BaseResponseDto<bool>.SuccessResult(isValid, "Environment validation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam environment");
                return BaseResponseDto<bool>.FailureResult("Failed to validate exam environment");
            }
        }

        public async Task<BaseResponseDto<bool>> ShouldTerminateExamAsync(int baithiId, CancellationToken cancellationToken = default)
        {
            try
            {
                var warningCount = await _canhbaoRepository.GetCountByBaithiIdAsync(baithiId);
                var shouldTerminate = warningCount >= 5; // Terminate after 5 warnings
                
                return BaseResponseDto<bool>.SuccessResult(shouldTerminate, "Termination check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking termination for exam session {ExamSessionId}", baithiId);
                return BaseResponseDto<bool>.FailureResult("Failed to check termination");
            }
        }

        public async Task<BaseResponseDto<ExamSecuritySummaryDto>> GetSecuritySummaryAsync(int baithiId, CancellationToken cancellationToken = default)
        {
            try
            {
                var warnings = await _canhbaoRepository.GetByBaithiIdAsync(baithiId);
                
                var summary = new ExamSecuritySummaryDto
                {
                    ExamSessionId = baithiId,
                    TotalWarnings = warnings.Count,
                    CriticalWarnings = warnings.Count(w => w.MucDoNghiemTrong == "High"),
                    LastWarningTime = warnings.LastOrDefault()?.ThoiGian,
                    SecurityStatus = warnings.Count >= 5 ? "Critical" : warnings.Count >= 3 ? "Warning" : "Normal"
                };

                return BaseResponseDto<ExamSecuritySummaryDto>.SuccessResult(summary, "Security summary retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security summary for exam session {ExamSessionId}", baithiId);
                return BaseResponseDto<ExamSecuritySummaryDto>.FailureResult("Failed to get security summary");
            }
        }
    }
}