using BanTayVang.API.DTOs.AntiCheat;
using BanTayVang.API.DTOs.Common;

namespace BanTayVang.API.Services.Interfaces.Security
{
    /// <summary>
    /// Service for exam security and anti-cheat operations
    /// Follows SRP - focused on security only
    /// </summary>
    public interface IExamSecurityService
    {
        /// <summary>
        /// Logs security events with OWASP compliance
        /// </summary>
        /// <param name="eventType">Type of security event</param>
        /// <param name="description">Event description</param>
        /// <param name="userId">User ID (optional)</param>
        /// <param name="severity">Event severity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> LogSecurityEventAsync(string eventType, string description, int? userId = null, string severity = "Info", CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs suspicious activity with OWASP security logging
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="loaiCanhBao">Warning type</param>
        /// <param name="moTa">Description</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets warning count for an exam session
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Warning count</returns>
        Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates exam environment for security
        /// </summary>
        /// <param name="environmentData">Environment data to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<BaseResponseDto<bool>> ValidateExamEnvironmentAsync(Dictionary<string, object> environmentData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if exam session should be terminated due to violations
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Termination recommendation</returns>
        Task<BaseResponseDto<bool>> ShouldTerminateExamAsync(int baithiId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets security summary for an exam session
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security summary</returns>
        Task<BaseResponseDto<ExamSecuritySummaryDto>> GetSecuritySummaryAsync(int baithiId, CancellationToken cancellationToken = default);
    }
}