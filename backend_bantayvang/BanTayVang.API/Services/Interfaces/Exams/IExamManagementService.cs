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
        /// <param name="nguoiTao">User creating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created exam details</returns>
        Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets exam by code with security validation
        /// </summary>
        /// <param name="maDeThi">Exam code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exam details</returns>
        Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets list of active exams
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active exams</returns>
        Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates exam configuration
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="nguoiCapNhat">User updating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated exam details</returns>
        Task<BaseResponseDto<DethiDto>> UpdateExamAsync(int examId, UpdateDethiDto updateDto, int nguoiCapNhat, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates an exam
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="nguoiCapNhat">User deactivating the exam</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BaseResponseDto> DeactivateExamAsync(int examId, int nguoiCapNhat, CancellationToken cancellationToken = default);
    }
}