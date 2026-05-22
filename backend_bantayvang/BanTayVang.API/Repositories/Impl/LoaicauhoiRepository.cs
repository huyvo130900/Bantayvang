using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class LoaicauhoiRepository : BaseRepository<Loaicauhoi>, ILoaicauhoiRepository
    {
        public LoaicauhoiRepository(BanTayVangDbContext context) : base(context) { }

        public async Task<bool> ExistsByNameAsync(string tenLoai, int? excludeId = null)
        {
            var query = _dbSet.Where(l => l.TenLoai == tenLoai);
            if (excludeId.HasValue)
                query = query.Where(l => l.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<int> GetQuestionCountAsync(int loaiId)
        {
            return await _context.Cauhois.CountAsync(c => c.IdLoaiCauHoi == loaiId && c.DaXoa != true);
        }
    }
}