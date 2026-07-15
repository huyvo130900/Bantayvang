using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamService
    {
        // Quản lý đề thi
        Task<BaseResponseDto<ExamPaperDto>> CreateExamAsync(CreateExamPaperDto createDto, int createdBy);
        Task<BaseResponseDto<ExamPaperDto>> GetExamByCodeAsync(string examPaperCode);
        Task<BaseResponseDto<List<ExamPaperDto>>> GetActiveExamsAsync();
        Task<BaseResponseDto<List<ExamPaperDto>>> GetAllExamsAsync(string? status = null, string? department = null);
        Task<BaseResponseDto<ExamPreviewDto>> GetExamPreviewAsync(int examId);

        Task<BaseResponseDto<ExamPaperDto>> UpdateExamAsync(int examId, UpdateExamPaperDto updateDto, int updatedBy);
        Task<BaseResponseDto<ExamPaperDto>> UpdateExamStatusAsync(int examId, string status, int updatedBy);
        Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa);

        // Bắt đầu thi
        Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int userId);
        Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int examSubmissionId, int userId);

        // Làm bài
        Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int userId);
        Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int examSubmissionId, int userId);

        // Nộp bài
        Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int userId);
        Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(); // Chạy background job

        // Kết quả của user hiện tại ← THÊM MỚI
        Task<BaseResponseDto<List<ExamSubmissionDto>>> GetMyResultsAsync(int userId);

        // Chống gian lận
        Task<BaseResponseDto> LogSuspiciousActivityAsync(int examSubmissionId, string warningType, string description);
        Task<BaseResponseDto<int>> GetWarningCountAsync(int examSubmissionId);
    }
}