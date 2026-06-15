using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IDethiRepository : IBaseRepository<Dethi>
    {
        Task<Dethi?> GetByMaDeThiAsync(string maDeThi, CancellationToken cancellationToken = default);
        Task<List<Dethi>> GetActiveExamsAsync(CancellationToken cancellationToken = default);
        Task<List<Dethi>> GetAllExamsAsync(string? trangThai = null, CancellationToken cancellationToken = default);
        Task<Dethi?> GetWithQuestionsAsync(int id);
        Task<bool> AddQuestionsToExamAsync(int dethiId, List<int> cauhoiIds);
        Task<Dethi?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UpdateExamQuestionsAsync(int examId, List<int> questionIds);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<List<Dethi>> GetExamsByKyThiAsync(int kyThiId, CancellationToken cancellationToken = default);
        Task<Dethi?> ResolveExamForCandidateAsync(int kyThiId, int taikhoanId, CancellationToken cancellationToken = default);
    }
}