using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class CheatWarningRepository : BaseRepository<CheatWarning>, ICheatWarningRepository
    {
        public CheatWarningRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<List<CheatWarning>> GetByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .Where(c => c.ExamSubmissionId == baithiId)
                .OrderByDescending(c => c.ActionTime)
                .ToListAsync();
        }

        public async Task<int> CountWarningsByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .CountAsync(c => c.ExamSubmissionId == baithiId);
        }

        public async Task<int> GetTotalWarningsAsync(int baithiId)
        {
            return await CountWarningsByBaiThiAsync(baithiId);
        }

        public async Task<int> GetCountByBaithiIdAsync(int baithiId)
        {
            return await _dbSet
                .CountAsync(c => c.ExamSubmissionId == baithiId);
        }

        public async Task<List<CheatWarning>> GetByBaithiIdAsync(int baithiId)
        {
            return await _dbSet
                .Where(c => c.ExamSubmissionId == baithiId)
                .OrderByDescending(c => c.ActionTime)
                .ToListAsync();
        }
    }
}