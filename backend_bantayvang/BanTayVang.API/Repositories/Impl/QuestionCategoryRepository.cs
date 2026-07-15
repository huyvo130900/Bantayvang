using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class QuestionCategoryRepository : BaseRepository<QuestionCategory>, IQuestionCategoryRepository
    {
        public QuestionCategoryRepository(BanTayVangDbContext context) : base(context) { }

        public async Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null)
        {
            var query = _dbSet.Where(l => l.CategoryName == categoryName);
            if (excludeId.HasValue)
                query = query.Where(l => l.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<int> GetQuestionCountAsync(int loaiId)
        {
            return await _context.Questions.CountAsync(c => c.QuestionCategoryId == loaiId && c.DaXoa != true);
        }
    }
}