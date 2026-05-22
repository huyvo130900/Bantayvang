using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamService
    {
        // Quản lý đề thi
        Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao);
        Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi);
        Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync();
        
        // Bắt đầu thi
        Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId);
        Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId);
        
        // Làm bài
        Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId);
        Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId);
        
        // Nộp bài
        Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId);
        Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(); // Chạy background job
        
        // Chống gian lận
        Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa);
        Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId);
    }
}