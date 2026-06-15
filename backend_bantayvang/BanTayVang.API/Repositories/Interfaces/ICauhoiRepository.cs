using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ICauhoiRepository : IBaseRepository<Cauhoi>
    {
        Task<PagedResultDto<Cauhoi>> GetFilteredAsync(QuestionFilterDto filter);
        Task<int> GetFilteredCountAsync(QuestionFilterDto filter);
        Task<List<Cauhoi>> GetByKhoaPhongAsync(string khoaPhong);
        Task<bool> SoftDeleteAsync(int id, int nguoiCapNhat);
        Task<List<Cauhoi>> GetRandomQuestionsAsync(int count);
        Task<Cauhoi?> GetWithChoicesAsync(int id);
        Task<Cauhoi?> FindDuplicateAsync(string noiDungChuan, string? khoaPhong);
        Task<List<int>> GetValidQuestionIdsAsync(List<int> questionIds);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}