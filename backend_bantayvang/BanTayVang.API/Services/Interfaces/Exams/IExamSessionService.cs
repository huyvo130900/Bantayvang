using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces.Exams
{
    /// <summary>
    /// Service for exam session operations (taking exams)
    /// Follows ISP - focused on exam sessions only
    /// </summary>
    public interface IExamSessionService
    {
        /// <summary>
        /// Starts a new exam session with security validation
        /// </summary>
        /// <param name="startDto">Exam start data</param>
        /// <param name="taikhoanId">User taking the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exam session details</returns>
        Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets exam questions for a session (without correct answers)
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of exam questions</returns>
        Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current exam progress
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exam progress details</returns>
        Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses an exam session
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> PauseExamAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes a paused exam session
        /// </summary>
        /// <param name="baithiId">Exam session ID</param>
        /// <param name="taikhoanId">User ID for security validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> ResumeExamAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default);
    }
}