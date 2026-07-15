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

        public async Task<List<CheatWarning>> GetByBaiThiAsync(int examSubmissionId)
        {
            return await _dbSet
                .Where(c => c.ExamSubmissionId == examSubmissionId)
                .OrderByDescending(c => c.ActionTime)
                .ToListAsync();
        }

        public async Task<int> CountWarningsByBaiThiAsync(int examSubmissionId)
        {
            return await _dbSet
                .CountAsync(c => c.ExamSubmissionId == examSubmissionId);
        }

        public async Task<int> GetTotalWarningsAsync(int examSubmissionId)
        {
            return await CountWarningsByBaiThiAsync(examSubmissionId);
        }

        public async Task<int> GetCountByBaithiIdAsync(int examSubmissionId)
        {
            return await _dbSet
                .CountAsync(c => c.ExamSubmissionId == examSubmissionId);
        }

        public async Task<List<CheatWarning>> GetByBaithiIdAsync(int examSubmissionId)
        {
            return await _dbSet
                .Where(c => c.ExamSubmissionId == examSubmissionId)
                .OrderByDescending(c => c.ActionTime)
                .ToListAsync();
        }
    }
}