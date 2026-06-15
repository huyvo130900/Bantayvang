using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces.Exams
{
    /// <summary>
    /// Service for exam answer submission operations
    /// Follows ISP - focused on submissions only
    /// </summary>
    public interface IExamSubmissionService
    {
        /// <summary>
        /// Saves an individual answer with validation
        /// </summary>
        /// <param name="answerDto">Answer data</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits complete exam with auto-grading
        /// </summary>
        /// <param name="submitDto">Submission data</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exam result</returns>
        Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-submits expired exams (background job)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates answer before saving
        /// </summary>
        /// <param name="answerDto">Answer to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<BaseResponseDto<bool>> ValidateAnswerAsync(SubmitAnswerDto answerDto, CancellationToken cancellationToken = default);
    }
}