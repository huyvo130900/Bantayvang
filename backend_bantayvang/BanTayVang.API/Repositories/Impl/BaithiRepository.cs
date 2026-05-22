using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class BaithiRepository : BaseRepository<Baithi>, IBaithiRepository
    {
        public BaithiRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<Baithi?> GetActiveExamSessionAsync(int taikhoanId, int dethiId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.IdTaiKhoan == taikhoanId 
                                       && b.IdDeThi == dethiId 
                                       && (b.TrangThai == "InProgress" || b.TrangThai == "Completed"));
        }

        public async Task<List<Baithi>> GetByTaiKhoanAsync(int taikhoanId)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Where(b => b.IdTaiKhoan == taikhoanId)
                .OrderByDescending(b => b.ThoiGianNop)
                .ToListAsync();
        }

        public async Task<Baithi?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.IdTaiKhoanNavigation)
                .Include(b => b.Chitietlambais)
                .Include(b => b.Canhbaogianlans)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> UpdateExamStatusAsync(int id, string trangThai)
        {
            try
            {
                var baithi = await GetByIdAsync(id);
                if (baithi == null)
                    return false;

                baithi.TrangThai = trangThai;
                if (trangThai == "Completed")
                {
                    baithi.ThoiGianNop = DateTime.Now;
                }

                await UpdateAsync(baithi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Baithi>> GetExpiredInProgressExamsAsync()
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Where(b => b.TrangThai == "InProgress" 
                         && b.IdDeThiNavigation != null
                         && b.IdDeThiNavigation.ThoiGianLamBai != null
                         && b.ThoiGianBatDau != null
                         && DateTime.Now > b.ThoiGianBatDau.Value.AddMinutes(b.IdDeThiNavigation.ThoiGianLamBai.Value))
                .ToListAsync();
        }

        public async Task<Baithi?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.IdTaiKhoanNavigation)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<int> GetActiveSessionsCountAsync(int userId)
        {
            return await _dbSet
                .CountAsync(b => b.IdTaiKhoan == userId && b.TrangThai == "InProgress");
        }

        public async Task<List<Baithi>> GetActiveSessionsByExamAsync(int examId)
        {
            return await _dbSet
                .Include(b => b.IdTaiKhoanNavigation)
                .Where(b => b.IdDeThi == examId && b.TrangThai == "InProgress")
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}