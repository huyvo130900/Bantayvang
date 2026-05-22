using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ILoaicauhoiRepository : IBaseRepository<Loaicauhoi>
    {
        Task<bool> ExistsByNameAsync(string tenLoai, int? excludeId = null);
        Task<int> GetQuestionCountAsync(int loaiId);
    }
}