using BanTayVang.API.Models;

namespace BanTayVang.API.Repositories.Interfaces
{
    public interface ISubmissionDetailRepository : IBaseRepository<SubmissionDetail>
    {
        Task<List<SubmissionDetail>> GetByBaiThiAsync(int examSubmissionId);
        Task<SubmissionDetail?> GetAnswerAsync(int examSubmissionId, int questionId);
        Task<bool> SaveAnswerAsync(SubmissionDetail chitiet);
        Task DeleteAnswersByQuestionAsync(int examSubmissionId, int questionId);
        Task<int> CountCorrectAnswersAsync(int examSubmissionId);
    }
}