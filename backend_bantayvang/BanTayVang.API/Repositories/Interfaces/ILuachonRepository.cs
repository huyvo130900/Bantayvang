using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ILuachonRepository : IBaseRepository<Luachon>
    {
        Task<List<Luachon>> GetByCauhoiIdAsync(int cauhoiId);
        Task<bool> DeleteByCauhoiIdAsync(int cauhoiId);
        Task<bool> DeleteByQuestionIdAsync(int questionId);
    }
}