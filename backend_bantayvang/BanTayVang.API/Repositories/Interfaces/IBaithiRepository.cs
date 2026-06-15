using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IBaithiRepository : IBaseRepository<Baithi>
    {
        Task<Baithi?> GetActiveExamSessionAsync(int taikhoanId, int dethiId);
        Task<List<Baithi>> GetByTaiKhoanAsync(int taikhoanId);
        Task<Baithi?> GetWithDetailsAsync(int id);
        Task<bool> UpdateExamStatusAsync(int id, string trangThai);
        Task<List<Baithi>> GetExpiredInProgressExamsAsync();
        Task<Baithi?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> GetActiveSessionsCountAsync(int userId);
        Task<List<Baithi>> GetActiveSessionsByExamAsync(int examId);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}