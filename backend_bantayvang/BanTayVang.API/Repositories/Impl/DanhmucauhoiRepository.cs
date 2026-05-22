using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class DanhmucauhoiRepository : BaseRepository<Danhmucauhoi>, IDanhmucauhoiRepository
    {
        public DanhmucauhoiRepository(BanTayVangDbContext context) : base(context) { }

        public async Task<bool> ExistsByNameAsync(string tenDanhMuc, int? excludeId = null)
        {
            var query = _dbSet.Where(d => d.TenDanhMuc == tenDanhMuc);
            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<int> GetQuestionCountAsync(int danhMucId)
        {
            return await _context.Cauhois.CountAsync(c => c.IdDanhMuc == danhMucId && c.DaXoa != true);
        }
    }
}