using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces.Validation
{
    /// <summary>
    /// Service for exam validation operations
    /// Follows SRP - focused on validation only
    /// Implements OWASP input validation standards
    /// </summary>
    public interface IExamValidationService
    {
        /// <summary>
        /// Validates exam creation data
        /// </summary>
        /// <param name="createDto">Exam creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResultDto> ValidateCreateExamAsync(CreateDethiDto createDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates exam update data
        /// </summary>
        /// <param name="updateDto">Exam update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResultDto> ValidateUpdateExamAsync(UpdateDethiDto updateDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates exam start request
        /// </summary>
        /// <param name="startDto">Exam start data</param>
        /// <param name="taikhoanId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResultDto> ValidateStartExamAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates answer submission
        /// </summary>
        /// <param name="answerDto">Answer data</param>
        /// <param name="taikhoanId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResultDto> ValidateAnswerSubmissionAsync(SubmitAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates exam submission
        /// </summary>
        /// <param name="submitDto">Submission data</param>
        /// <param name="taikhoanId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResultDto> ValidateExamSubmissionAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates user permissions for exam operations
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="operation">Operation type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Permission validation result</returns>
        Task<ValidationResultDto> ValidateExamPermissionAsync(int examId, int userId, string operation, CancellationToken cancellationToken = default);
    }
}