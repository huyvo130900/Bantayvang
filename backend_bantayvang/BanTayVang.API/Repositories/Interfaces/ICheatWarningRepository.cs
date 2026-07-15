using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ICheatWarningRepository : IBaseRepository<CheatWarning>
    {
        Task<List<CheatWarning>> GetByBaiThiAsync(int baithiId);
        Task<int> CountWarningsByBaiThiAsync(int baithiId);
        Task<int> GetTotalWarningsAsync(int baithiId);
        Task<int> GetCountByBaithiIdAsync(int baithiId);
        Task<List<CheatWarning>> GetByBaithiIdAsync(int baithiId);
    }
}