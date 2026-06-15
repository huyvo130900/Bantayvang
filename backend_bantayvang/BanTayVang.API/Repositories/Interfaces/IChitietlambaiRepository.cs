using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IChitietlambaiRepository : IBaseRepository<Chitietlambai>
    {
        Task<List<Chitietlambai>> GetByBaiThiAsync(int baithiId);
        Task<Chitietlambai?> GetAnswerAsync(int baithiId, int cauhoiId);
        Task<bool> SaveAnswerAsync(Chitietlambai chitiet);
        Task<int> CountCorrectAnswersAsync(int baithiId);
    }
}