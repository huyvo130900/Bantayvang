using BanTayVang.API.DTOs.Common;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<PagedResultDto<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}