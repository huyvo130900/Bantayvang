using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class CanhbaogianlanRepository : BaseRepository<Canhbaogianlan>, ICanhbaogianlanRepository
    {
        public CanhbaogianlanRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<List<Canhbaogianlan>> GetByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .Where(c => c.IdBaiThi == baithiId)
                .OrderByDescending(c => c.ThoiGian)
                .ToListAsync();
        }

        public async Task<int> CountWarningsByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .CountAsync(c => c.IdBaiThi == baithiId);
        }

        public async Task<int> GetTotalWarningsAsync(int baithiId)
        {
            return await CountWarningsByBaiThiAsync(baithiId);
        }

        public async Task<int> GetCountByBaithiIdAsync(int baithiId)
        {
            return await _dbSet
                .CountAsync(c => c.IdBaiThi == baithiId);
        }

        public async Task<List<Canhbaogianlan>> GetByBaithiIdAsync(int baithiId)
        {
            return await _dbSet
                .Where(c => c.IdBaiThi == baithiId)
                .OrderByDescending(c => c.ThoiGian)
                .ToListAsync();
        }
    }
}