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

        public async Task<ExamSubmission?> GetActiveExamSessionAsync(int userId, int ExamPaperId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.UserId == userId 
                                       && b.ExamPaperId == ExamPaperId 
                                       && (b.Status == "InProgress" || b.Status == "Paused"));
        }

        public async Task<List<ExamSubmission>> GetByTaiKhoanAsync(int userId)
        {
            return await _dbSet
                .Include(b => b.ExamPaper)
                .Include(b => b.ExamCampaign)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.SubmitTime)
                .ToListAsync();
        }

        public async Task<ExamSubmission?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(b => b.ExamPaper)
                .Include(b => b.User)
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
                    examSubmission.SubmitTime = DateTime.UtcNow.AddHours(7);
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
            // BUG FIX: this used to ignore ExamAssignment.ExtraMinutes entirely, so a supervisor
            // granting a student extra time via "extend-time" had zero effect - this job would
            // still auto-submit the student's exam at the ORIGINAL duration, right on schedule.
            // Now folds in the per-user grant (if any, and only while IsActive) before comparing.
            var now = DateTime.UtcNow.AddHours(7);
            return await _dbSet
                .Include(b => b.User)
                .Include(b => b.ExamPaper)
                .Where(b => b.Status == "InProgress"
                         && b.ExamPaper != null
                         && b.ExamPaper.DurationMinutes != null
                         && b.StartTime != null
                         && now > b.StartTime.Value.AddMinutes(b.ExamPaper.DurationMinutes.Value +
                                (_context.ExamAssignments
                                    .Where(a => a.UserId == b.UserId && a.ExamId == b.ExamPaperId && a.IsActive)
                                    .Select(a => a.ExtraMinutes ?? 0)
                                    .FirstOrDefault())))
                .ToListAsync();
        }

        public async Task<ExamSubmission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(b => b.ExamPaper)
                .Include(b => b.User)
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
                .Include(b => b.User)
                .Where(b => b.ExamPaperId == examId && b.Status == "InProgress")
                .ToListAsync();
        }

        public async Task<List<ExamSubmission>> GetActiveSessionsByCampaignAsync(int examCampaignId)
        {
            return await _dbSet
                .Include(b => b.User)
                .Include(b => b.ExamPaper)
                .Where(b => b.ExamCampaignId == examCampaignId && b.Status == "InProgress")
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
