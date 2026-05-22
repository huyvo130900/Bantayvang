using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IDanhmucauhoiRepository : IBaseRepository<Danhmucauhoi>
    {
        Task<bool> ExistsByNameAsync(string tenDanhMuc, int? excludeId = null);
        Task<int> GetQuestionCountAsync(int danhMucId);
    }
}