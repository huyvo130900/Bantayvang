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
        Task<bool> AddQuestionsToExamAsync(int ExamPaperId, List<int> questionIds);
        Task<ExamPaper?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UpdateExamQuestionsAsync(int examId, List<int> questionIds);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<List<ExamPaper>> GetExamsByKyThiAsync(int examCampaignId, CancellationToken cancellationToken = default);
        Task<ExamPaper?> ResolveExamForCandidateAsync(int examCampaignId, int userId, CancellationToken cancellationToken = default);
    }
}
