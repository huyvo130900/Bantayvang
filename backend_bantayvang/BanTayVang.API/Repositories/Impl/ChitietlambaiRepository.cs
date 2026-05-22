using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class ChitietlambaiRepository : BaseRepository<Chitietlambai>, IChitietlambaiRepository
    {
        public ChitietlambaiRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<List<Chitietlambai>> GetByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .Include(c => c.IdCauHoiNavigation)
                .Include(c => c.IdLuaChonDaChonNavigation)
                .Where(c => c.IdBaiThi == baithiId)
                .ToListAsync();
        }

        public async Task<Chitietlambai?> GetAnswerAsync(int baithiId, int cauhoiId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.IdBaiThi == baithiId && c.IdCauHoi == cauhoiId);
        }

        public async Task<bool> SaveAnswerAsync(Chitietlambai chitiet)
        {
            try
            {
                var existing = await GetAnswerAsync(chitiet.IdBaiThi!.Value, chitiet.IdCauHoi!.Value);
                
                if (existing != null)
                {
                    // Update existing answer
                    existing.IdLuaChonDaChon = chitiet.IdLuaChonDaChon;
                    existing.CauTraLoiTuLuan = chitiet.CauTraLoiTuLuan;
                    existing.ThoiGianTraLoi = chitiet.ThoiGianTraLoi;
                    existing.DaLuu = chitiet.DaLuu;
                    existing.DiemDatDuoc = chitiet.DiemDatDuoc;
                    
                    await UpdateAsync(existing);
                }
                else
                {
                    // Add new answer
                    await AddAsync(chitiet);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> CountCorrectAnswersAsync(int baithiId)
        {
            var answers = await _dbSet
                .Include(c => c.IdLuaChonDaChonNavigation)
                .Where(c => c.IdBaiThi == baithiId && c.IdLuaChonDaChon.HasValue)
                .ToListAsync();

            return answers.Count(a => a.IdLuaChonDaChonNavigation?.LaDapAnDung == true);
        }
    }
}