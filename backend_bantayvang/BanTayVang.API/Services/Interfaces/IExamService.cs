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
        Task<BaseResponseDto<List<DethiDto>>> GetAllExamsAsync(string? trangThai = null, string? khoaPhong = null);
        Task<BaseResponseDto<ExamPreviewDto>> GetExamPreviewAsync(int examId);

        Task<BaseResponseDto<DethiDto>> UpdateExamAsync(int examId, UpdateDethiDto updateDto, int nguoiCapNhat);
        Task<BaseResponseDto<DethiDto>> UpdateExamStatusAsync(int examId, string trangThai, int nguoiCapNhat);
        Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa);

        // Bắt đầu thi
        Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId);
        Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId);

        // Làm bài
        Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId);
        Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId);

        // Nộp bài
        Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId);
        Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(); // Chạy background job

        // Kết quả của user hiện tại ← THÊM MỚI
        Task<BaseResponseDto<List<BaithiDto>>> GetMyResultsAsync(int taikhoanId);

        // Chống gian lận
        Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa);
        Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId);
    }
}