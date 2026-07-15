using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ISubmissionDetailRepository : IBaseRepository<SubmissionDetail>
    {
        Task<List<SubmissionDetail>> GetByBaiThiAsync(int baithiId);
        Task<SubmissionDetail?> GetAnswerAsync(int baithiId, int cauhoiId);
        Task<bool> SaveAnswerAsync(SubmissionDetail chitiet);
        Task DeleteAnswersByQuestionAsync(int baithiId, int cauhoiId);
        Task<int> CountCorrectAnswersAsync(int baithiId);
    }
}