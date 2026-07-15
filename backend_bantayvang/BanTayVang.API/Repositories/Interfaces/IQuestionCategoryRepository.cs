using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IQuestionCategoryRepository : IBaseRepository<QuestionCategory>
    {
        Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null);
        Task<int> GetQuestionCountAsync(int loaiId);
    }
}