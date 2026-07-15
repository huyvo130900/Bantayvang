using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IExamSubmissionRepository : IBaseRepository<ExamSubmission>
    {
        Task<ExamSubmission?> GetActiveExamSessionAsync(int taikhoanId, int dethiId);
        Task<List<ExamSubmission>> GetByTaiKhoanAsync(int taikhoanId);
        Task<ExamSubmission?> GetWithDetailsAsync(int id);
        Task<bool> UpdateExamStatusAsync(int id, string status);
        Task<List<ExamSubmission>> GetExpiredInProgressExamsAsync();
        Task<ExamSubmission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> GetActiveSessionsCountAsync(int userId);
        Task<List<ExamSubmission>> GetActiveSessionsByExamAsync(int examId);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}