using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class SubmissionDetailRepository : BaseRepository<SubmissionDetail>, ISubmissionDetailRepository
    {
        public SubmissionDetailRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<List<SubmissionDetail>> GetByBaiThiAsync(int baithiId)
        {
            return await _dbSet
                .Include(c => c.IdCauHoiNavigation)
                    .ThenInclude(ch => ch!.QuestionOptions)
                .Include(c => c.IdCauHoiNavigation)
                    .ThenInclude(ch => ch!.IdLoaiCauHoiNavigation)
                .Include(c => c.IdLuaChonDaChonNavigation)
                .Where(c => c.ExamSubmissionId == baithiId)
                .ToListAsync();
        }

        public async Task<SubmissionDetail?> GetAnswerAsync(int baithiId, int cauhoiId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.ExamSubmissionId == baithiId && c.QuestionId == cauhoiId);
        }

        public async Task<bool> SaveAnswerAsync(SubmissionDetail chitiet)
        {
            try
            {
                var existing = await GetAnswerAsync(chitiet.ExamSubmissionId!.Value, chitiet.QuestionId!.Value);
                
                if (existing != null)
                {
                    // Update existing answer
                    existing.SelectedOptionId = chitiet.SelectedOptionId;
                    existing.CauTraLoiTuLuan = chitiet.CauTraLoiTuLuan;
                    existing.ThoiGianTraLoi = chitiet.ThoiGianTraLoi;
                    existing.DaLuu = chitiet.DaLuu;
                    existing.ScoreObtained = chitiet.ScoreObtained;
                    
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

        public async Task DeleteAnswersByQuestionAsync(int baithiId, int cauhoiId)
        {
            var records = await _dbSet
                .Where(c => c.ExamSubmissionId == baithiId && c.QuestionId == cauhoiId)
                .ToListAsync();

            if (records.Any())
            {
                _dbSet.RemoveRange(records);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountCorrectAnswersAsync(int baithiId)
        {
            var answers = await _dbSet
                .Include(c => c.IdLuaChonDaChonNavigation)
                .Where(c => c.ExamSubmissionId == baithiId && c.SelectedOptionId.HasValue)
                .ToListAsync();

            return answers.Count(a => a.IdLuaChonDaChonNavigation?.IsCorrect == true);
        }
    }
}