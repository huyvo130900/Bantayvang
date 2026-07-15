using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IQuestionOptionRepository : IBaseRepository<QuestionOption>
    {
        Task<List<QuestionOption>> GetByCauhoiIdAsync(int cauhoiId);
        Task<bool> DeleteByCauhoiIdAsync(int cauhoiId);
        Task<bool> DeleteByQuestionIdAsync(int questionId);
    }
}