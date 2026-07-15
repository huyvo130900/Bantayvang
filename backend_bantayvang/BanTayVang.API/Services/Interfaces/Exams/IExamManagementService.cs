using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces.Exams
{
    /// <summary>
    /// Service for exam creation and management operations
    /// Follows ISP - focused on exam management only
    /// </summary>
    public interface IExamManagementService
    {
        /// <summary>
        /// Creates a new exam with questions
        /// </summary>
        /// <param name="createDto">Exam creation data</param>
        /// <param name="createdBy">User creating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created exam details</returns>
        Task<BaseResponseDto<ExamPaperDto>> CreateExamAsync(CreateExamPaperDto createDto, int createdBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets exam by code with security validation
        /// </summary>
        /// <param name="examPaperCode">Exam code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exam details</returns>
        Task<BaseResponseDto<ExamPaperDto>> GetExamByCodeAsync(string examPaperCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets list of active exams
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active exams</returns>
        Task<BaseResponseDto<List<ExamPaperDto>>> GetActiveExamsAsync(CancellationToken cancellationToken = default);

        Task<BaseResponseDto<List<ExamPaperDto>>> GetAllExamsAsync(string? status = null, CancellationToken cancellationToken = default);

        Task<BaseResponseDto<ExamPaperDto>> UpdateExamStatusAsync(int examId, string status, int updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates exam configuration
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="updatedBy">User updating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated exam details</returns>
        Task<BaseResponseDto<ExamPaperDto>> UpdateExamAsync(int examId, UpdateExamPaperDto updateDto, int updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates an exam
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="updatedBy">User deactivating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa, CancellationToken cancellationToken = default);
        Task<BaseResponseDto> DeactivateExamAsync(int examId, int updatedBy, CancellationToken cancellationToken = default);
    }
}