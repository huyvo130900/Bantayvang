using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;

namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Service for grading and result reporting
    /// </summary>
    public interface IGradingService
    {
        /// <summary>
        /// Lấy chi tiết kết quả bài thi
        /// </summary>
        Task<BaseResponseDto<ExamResultDetailDto>> GetResultDetailAsync(int baiThiId);

        /// <summary>
        /// Lấy danh sách kết quả thi của một đề
        /// </summary>
        Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByExamAsync(int examId);

        /// <summary>
        /// Chấm lại bài thi (auto-grading)
        /// </summary>
        Task<BaseResponseDto<ExamResultDetailDto>> RegradeAsync(int baiThiId);

        /// <summary>
        /// Chấm thủ công (cho câu tự luận)
        /// </summary>
        Task<BaseResponseDto> ManualGradeAsync(ManualGradingDto dto);

        /// <summary>
        /// Lấy bảng xếp hạng theo đề thi
        /// </summary>
        Task<BaseResponseDto<List<ExamResultDetailDto>>> GetRankingByExamAsync(int examId, int top = 50);

        /// <summary>
        /// Auto-grade tất cả bài thi đã hoàn thành nhưng chưa được chấm điểm
        /// </summary>
        Task<BaseResponseDto<int>> AutoGradeAllAsync();
    }
}