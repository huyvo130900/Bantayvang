using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ICheatWarningRepository : IBaseRepository<CheatWarning>
    {
        Task<List<CheatWarning>> GetByBaiThiAsync(int examSubmissionId);
        Task<int> CountWarningsByBaiThiAsync(int examSubmissionId);
        Task<int> GetTotalWarningsAsync(int examSubmissionId);
        Task<int> GetCountByExamSubmissionIdAsync(int examSubmissionId);
        Task<List<CheatWarning>> GetByExamSubmissionIdAsync(int examSubmissionId);
    }
}
