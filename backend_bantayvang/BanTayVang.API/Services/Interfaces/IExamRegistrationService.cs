using System.Collections.Generic;
using System.Threading.Tasks;
using BanTayVang.API.DTOs.ExamRegistration;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamRegistrationService
    {
        // BUG FIX: returns null (instead of throwing) when the CCCD already has an account or an
        // existing Pending application - see CreateAsync for why revealing that distinction to an
        // anonymous, unauthenticated caller is itself a PII-disclosure bug, independent of who
        // actually owns the CCCD being probed.
        Task<ExamRegistrationDto?> CreateAsync(CreateExamRegistrationDto dto);
        Task<IEnumerable<ExamRegistrationDto>> GetPendingRegistrationsAsync(int? departmentId = null);
        Task<bool> ApproveAsync(int id, int userId);
        Task<bool> RejectAsync(int id, string? reason, int userId);
    }
}
