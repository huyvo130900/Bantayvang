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

        public async Task<List<SubmissionDetail>> GetByBaiThiAsync(int examSubmissionId)
        {
            return await _dbSet
                .Include(c => c.Question)
                    .ThenInclude(ch => ch!.QuestionOptions)
                .Include(c => c.Question)
                    .ThenInclude(ch => ch!.QuestionCategory)
                .Include(c => c.SelectedOption)
                .Where(c => c.ExamSubmissionId == examSubmissionId)
                .ToListAsync();
        }

        public async Task<SubmissionDetail?> GetAnswerAsync(int examSubmissionId, int questionId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.ExamSubmissionId == examSubmissionId && c.QuestionId == questionId);
        }

        public async Task<bool> SaveAnswerAsync(SubmissionDetail detail)
        {
            try
            {
                var existing = await GetAnswerAsync(detail.ExamSubmissionId!.Value, detail.QuestionId!.Value);
                
                if (existing != null)
                {
                    // Update existing answer
                    existing.SelectedOptionId = detail.SelectedOptionId;
                    existing.EssayAnswer = detail.EssayAnswer;
                    existing.EssayImageUrl = detail.EssayImageUrl;
                    existing.AnswerTime = detail.AnswerTime;
                    existing.IsSaved = detail.IsSaved;
                    existing.ScoreObtained = detail.ScoreObtained;
                    
                    await UpdateAsync(existing);

                }
                else
                {
                    // Add new answer
                    await AddAsync(detail);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task DeleteAnswersByQuestionAsync(int examSubmissionId, int questionId)
        {
            var records = await _dbSet
                .Where(c => c.ExamSubmissionId == examSubmissionId && c.QuestionId == questionId)
                .ToListAsync();

            if (records.Any())
            {
                _dbSet.RemoveRange(records);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountCorrectAnswersAsync(int examSubmissionId)
        {
            var answers = await _dbSet
                .Include(c => c.SelectedOption)
                .Where(c => c.ExamSubmissionId == examSubmissionId && c.SelectedOptionId.HasValue)
                .ToListAsync();

            return answers.Count(a => a.SelectedOption?.IsCorrect == true);
        }
    }
}