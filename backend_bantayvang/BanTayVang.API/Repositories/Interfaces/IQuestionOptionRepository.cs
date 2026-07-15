using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IQuestionOptionRepository : IBaseRepository<QuestionOption>
    {
        Task<List<QuestionOption>> GetByCauhoiIdAsync(int questionId);
        Task<bool> DeleteByCauhoiIdAsync(int questionId);
        Task<bool> DeleteByQuestionIdAsync(int questionId);
    }
}