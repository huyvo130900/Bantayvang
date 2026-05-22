using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;

namespace BanTayVang.API.Services.Interfaces
{
    public interface IExamAssignmentService
    {
        Task<BaseResponseDto<List<ExamAssignmentDto>>> GetAssignmentsByExamAsync(int examId);
        Task<BaseResponseDto<List<ExamAssignmentDto>>> GetAssignmentsByUserAsync(int userId);
        Task<BaseResponseDto<int>> AssignUsersToExamAsync(CreateExamAssignmentDto dto);
        Task<BaseResponseDto> RemoveAssignmentAsync(int assignmentId);
        Task<BaseResponseDto<bool>> IsUserAssignedAsync(int examId, int userId);
        Task<BaseResponseDto> ExtendExamTimeAsync(ExtendExamTimeDto dto);
    }
}