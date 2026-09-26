using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface IQuestionRepository : IBaseRepository<Question>
    {
        Task<PagedResultDto<Question>> GetFilteredAsync(QuestionFilterDto filter);
        Task<int> GetFilteredCountAsync(QuestionFilterDto filter);
        Task<List<Question>> GetByKhoaPhongAsync(string department);
        Task<bool> SoftDeleteAsync(int id, int updatedBy);
        Task<List<Question>> GetRandomQuestionsAsync(int count, int? categoryId = null);
        Task<Question?> GetWithChoicesAsync(int id);
        Task<Question?> FindDuplicateAsync(string standardizedContent, string? department);
        Task<List<int>> GetValidQuestionIdsAsync(List<int> questionIds);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}