using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IExamPaperRepository : IBaseRepository<ExamPaper>
    {
        Task<ExamPaper?> GetByMaDeThiAsync(string examPaperCode, CancellationToken cancellationToken = default);
        Task<List<ExamPaper>> GetActiveExamsAsync(CancellationToken cancellationToken = default);
        Task<List<ExamPaper>> GetAllExamsAsync(string? status = null, CancellationToken cancellationToken = default);
        Task<ExamPaper?> GetWithQuestionsAsync(int id);
        Task<bool> AddQuestionsToExamAsync(int dethiId, List<int> cauhoiIds);
        Task<ExamPaper?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UpdateExamQuestionsAsync(int examId, List<int> questionIds);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<List<ExamPaper>> GetExamsByKyThiAsync(int kyThiId, CancellationToken cancellationToken = default);
        Task<ExamPaper?> ResolveExamForCandidateAsync(int kyThiId, int taikhoanId, CancellationToken cancellationToken = default);
    }
}