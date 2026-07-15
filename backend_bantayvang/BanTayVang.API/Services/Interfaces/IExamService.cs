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
        Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int taikhoanId);
        Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId);

        // Làm bài
        Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId);
        Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int baithiId, int taikhoanId);

        // Nộp bài
        Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId);
        Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(); // Chạy background job

        // Kết quả của user hiện tại ← THÊM MỚI
        Task<BaseResponseDto<List<ExamSubmissionDto>>> GetMyResultsAsync(int taikhoanId);

        // Chống gian lận
        Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa);
        Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId);
    }
}