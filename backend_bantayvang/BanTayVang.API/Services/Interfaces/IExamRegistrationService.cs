using System.Collections.Generic;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.ExamRegistration;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamRegistrationService
    {
        Task<ExamRegistrationDto> CreateAsync(CreateExamRegistrationDto dto);
        Task<IEnumerable<ExamRegistrationDto>> GetPendingRegistrationsAsync(int? departmentId = null);
        Task<bool> ApproveAsync(int id, int userId);
        Task<bool> RejectAsync(int id, string? reason, int userId);
    }
}
