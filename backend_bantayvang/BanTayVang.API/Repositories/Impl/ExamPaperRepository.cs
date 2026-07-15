using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class ExamPaperRepository : BaseRepository<ExamPaper>, IExamPaperRepository
    {
        public ExamPaperRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<ExamPaper?> GetByMaDeThiAsync(string examPaperCode)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.ExamPaperCode == examPaperCode);
        }

        public async Task<ExamPaper?> GetByMaDeThiAsync(string examPaperCode, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.ExamPaperCode == examPaperCode, cancellationToken);
        }

        public async Task<List<ExamPaper>> GetActiveExamsAsync()
        {
            return await _dbSet
                .Where(d => d.Status == "Active" || d.Status == "Published")
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ExamPaper>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.Status == "Active" || d.Status == "Published")
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ExamPaper?> GetWithQuestionsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.ExamPaperQuestions)
                    .ThenInclude(dc => dc.IdCauHoiNavigation)
                        .ThenInclude(c => c!.QuestionOptions)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<ExamPaper?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.ExamPaperQuestions)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<bool> AddQuestionsToExamAsync(int dethiId, List<int> cauhoiIds)
        {
            try
            {
                // Xóa câu hỏi cũ
                var existingQuestions = await _context.ExamPaperQuestions
                    .Where(dc => dc.ExamPaperId == dethiId)
                    .ToListAsync();
                _context.ExamPaperQuestions.RemoveRange(existingQuestions);

                // Thêm câu hỏi mới
                foreach (var cauhoiId in cauhoiIds)
                {
                    _context.ExamPaperQuestions.Add(new ExamPaperQuestion
                    {
                        ExamPaperId = dethiId,
                        QuestionId = cauhoiId
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
                var existingQuestions = await _context.ExamPaperQuestions
                    .Where(dc => dc.ExamPaperId == examId)
                    .ToListAsync();
                _context.ExamPaperQuestions.RemoveRange(existingQuestions);

                // Add new questions
                foreach (var questionId in questionIds)
                {
                    _context.ExamPaperQuestions.Add(new ExamPaperQuestion
                    {
                        ExamPaperId = examId,
                        QuestionId = questionId
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

        public async Task<List<ExamPaper>> GetAllExamsAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(d => d.ExamPaperQuestions)
                .AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status == status);
            return await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ExamPaper>> GetExamsByKyThiAsync(int kyThiId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.KyThiId == kyThiId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ExamPaper?> ResolveExamForCandidateAsync(int kyThiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            var kyThiExams = await _dbSet
                .Where(d => d.KyThiId == kyThiId)
                .OrderBy(d => d.Id)
                .ToListAsync(cancellationToken);

            if (!kyThiExams.Any())
            {
                return null;
            }

            // 1. Check if the candidate has an active (InProgress/Paused) session in this ExamCampaign.
            // If they do, they must resume it, so return the exam associated with that session.
            var activeSession = await _context.ExamSubmissions
                .Where(b => b.UserId == taikhoanId 
                         && b.ExamCampaignId == kyThiId 
                         && b.ExamPaperId.HasValue
                         && (b.Status == "InProgress" || b.Status == "Paused"))
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSession != null)
            {
                var exam = kyThiExams.FirstOrDefault(d => d.Id == activeSession.ExamPaperId!.Value);
                if (exam != null)
                {
                    return exam;
                }
            }

            // 2. Candidate is starting a new attempt.
            // Retrieve all past sessions of this candidate in this ExamCampaign.
            var pastSessions = await _context.ExamSubmissions
                .Where(b => b.UserId == taikhoanId 
                         && b.ExamCampaignId == kyThiId 
                         && b.ExamPaperId.HasValue)
                .ToListAsync(cancellationToken);

            // Group and count attempts per exam ID
            var examAttemptCounts = pastSessions
                .GroupBy(b => b.ExamPaperId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // 3. Determine the preferred starting exam index for the candidate
            int startIndex = -1;
            var examCampaign = await _context.Set<ExamCampaign>().FindAsync(new object[] { kyThiId }, cancellationToken);
            if (examCampaign != null && examCampaign.DepartmentId.HasValue)
            {
                var khoa = await _context.Set<Department>().FindAsync(new object[] { examCampaign.DepartmentId.Value }, cancellationToken);
                if (khoa != null)
                {
                    var students = await _context.Users
                        .Where(u => u.Department == khoa.DepartmentName && u.RoleId == 3)
                        .OrderBy(u => u.Id)
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);

                    var studentIndex = students.IndexOf(taikhoanId);
                    if (studentIndex >= 0)
                    {
                        startIndex = studentIndex % kyThiExams.Count;
                    }
                }
            }

            if (startIndex == -1)
            {
                startIndex = taikhoanId % kyThiExams.Count;
            }

            // 4. Sort the exams by the number of attempts (ascending), and then by the preferred sequence order (ascending)
            int N = kyThiExams.Count;
            var sortedExams = kyThiExams
                .Select((exam, i) => new
                {
                    Exam = exam,
                    Attempts = examAttemptCounts.TryGetValue(exam.Id, out var count) ? count : 0,
                    OrderIndex = (i - startIndex + N) % N
                })
                .OrderBy(x => x.Attempts)
                .ThenBy(x => x.OrderIndex)
                .Select(x => x.Exam)
                .ToList();

            return sortedExams.FirstOrDefault();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet
                .Include(d => d.ExamPaperQuestions)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null)
                return false;

            // Delete associated ExamPaperQuestions first to prevent FK violation
            if (entity.ExamPaperQuestions != null && entity.ExamPaperQuestions.Any())
            {
                _context.ExamPaperQuestions.RemoveRange(entity.ExamPaperQuestions);
            }

            // Delete associated ExamAssignments if any
            var assignments = await _context.ExamAssignments
                .Where(a => a.ExamId == id)
                .ToListAsync();
            if (assignments.Any())
            {
                _context.ExamAssignments.RemoveRange(assignments);
            }

            // Delete associated ExamSubmissions and their children (CheatWarnings and SubmissionDetails) if any
            var examSubmissions = await _context.ExamSubmissions
                .Include(b => b.CheatWarnings)
                .Include(b => b.SubmissionDetails)
                .Where(b => b.ExamPaperId == id)
                .ToListAsync();
            if (examSubmissions.Any())
            {
                foreach (var bt in examSubmissions)
                {
                    if (bt.CheatWarnings != null && bt.CheatWarnings.Any())
                        _context.CheatWarnings.RemoveRange(bt.CheatWarnings);
                    if (bt.SubmissionDetails != null && bt.SubmissionDetails.Any())
                        _context.SubmissionDetails.RemoveRange(bt.SubmissionDetails);
                }
                _context.ExamSubmissions.RemoveRange(examSubmissions);
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}