using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ICanhbaogianlanRepository : IBaseRepository<Canhbaogianlan>
    {
        Task<List<Canhbaogianlan>> GetByBaiThiAsync(int baithiId);
        Task<int> CountWarningsByBaiThiAsync(int baithiId);
        Task<int> GetTotalWarningsAsync(int baithiId);
        Task<int> GetCountByBaithiIdAsync(int baithiId);
        Task<List<Canhbaogianlan>> GetByBaithiIdAsync(int baithiId);
    }
}