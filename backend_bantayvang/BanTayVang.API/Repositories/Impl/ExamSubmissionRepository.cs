using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class ExamSubmissionRepository : BaseRepository<ExamSubmission>, IExamSubmissionRepository
    {
        public ExamSubmissionRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<ExamSubmission?> GetActiveExamSessionAsync(int taikhoanId, int dethiId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.UserId == taikhoanId 
                                       && b.ExamPaperId == dethiId 
                                       && (b.Status == "InProgress" || b.Status == "Paused"));
        }

        public async Task<List<ExamSubmission>> GetByTaiKhoanAsync(int taikhoanId)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Where(b => b.UserId == taikhoanId)
                .OrderByDescending(b => b.SubmitTime)
                .ToListAsync();
        }

        public async Task<ExamSubmission?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.IdTaiKhoanNavigation)
                .Include(b => b.SubmissionDetails)
                .Include(b => b.CheatWarnings)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> UpdateExamStatusAsync(int id, string status)
        {
            try
            {
                var examSubmission = await GetByIdAsync(id);
                if (examSubmission == null)
                    return false;

                examSubmission.Status = status;
                if (status == "Completed")
                {
                    examSubmission.SubmitTime = DateTime.Now;
                }

                await UpdateAsync(examSubmission);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ExamSubmission>> GetExpiredInProgressExamsAsync()
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Where(b => b.Status == "InProgress" 
                         && b.IdDeThiNavigation != null
                         && b.IdDeThiNavigation.DurationMinutes != null
                         && b.StartTime != null
                         && DateTime.Now > b.StartTime.Value.AddMinutes(b.IdDeThiNavigation.DurationMinutes.Value))
                .ToListAsync();
        }

        public async Task<ExamSubmission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(b => b.IdDeThiNavigation)
                .Include(b => b.IdTaiKhoanNavigation)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<int> GetActiveSessionsCountAsync(int userId)
        {
            return await _dbSet
                .CountAsync(b => b.UserId == userId && b.Status == "InProgress");
        }

        public async Task<List<ExamSubmission>> GetActiveSessionsByExamAsync(int examId)
        {
            return await _dbSet
                .Include(b => b.IdTaiKhoanNavigation)
                .Where(b => b.ExamPaperId == examId && b.Status == "InProgress")
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}