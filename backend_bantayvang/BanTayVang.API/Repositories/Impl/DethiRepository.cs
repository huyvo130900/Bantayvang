using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class DethiRepository : BaseRepository<Dethi>, IDethiRepository
    {
        public DethiRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<Dethi?> GetByMaDeThiAsync(string maDeThi)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi);
        }

        public async Task<Dethi?> GetByMaDeThiAsync(string maDeThi, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi, cancellationToken);
        }

        public async Task<List<Dethi>> GetActiveExamsAsync()
        {
            return await _dbSet
                .Where(d => d.TrangThai == "Active" || d.TrangThai == "Published")
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<List<Dethi>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.TrangThai == "Active" || d.TrangThai == "Published")
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dethi?> GetWithQuestionsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.DethiCauhois)
                    .ThenInclude(dc => dc.IdCauHoiNavigation)
                        .ThenInclude(c => c!.Luachons)
                .Include(d => d.DethiCauhois)
                    .ThenInclude(dc => dc.IdCauHoiNavigation)
                        .ThenInclude(c => c!.IdDanhMucNavigation)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Dethi?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.DethiCauhois)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<bool> AddQuestionsToExamAsync(int dethiId, List<int> cauhoiIds)
        {
            try
            {
                // Xóa câu hỏi cũ
                var existingQuestions = await _context.DethiCauhois
                    .Where(dc => dc.IdDeThi == dethiId)
                    .ToListAsync();
                _context.DethiCauhois.RemoveRange(existingQuestions);

                // Thêm câu hỏi mới
                foreach (var cauhoiId in cauhoiIds)
                {
                    _context.DethiCauhois.Add(new DethiCauhoi
                    {
                        IdDeThi = dethiId,
                        IdCauHoi = cauhoiId
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateExamQuestionsAsync(int examId, List<int> questionIds)
        {
            try
            {
                // Remove existing questions
                var existingQuestions = await _context.DethiCauhois
                    .Where(dc => dc.IdDeThi == examId)
                    .ToListAsync();
                _context.DethiCauhois.RemoveRange(existingQuestions);

                // Add new questions
                foreach (var questionId in questionIds)
                {
                    _context.DethiCauhois.Add(new DethiCauhoi
                    {
                        IdDeThi = examId,
                        IdCauHoi = questionId
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}