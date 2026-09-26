using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Exam assignment controller - admin assigns users to exams & extends time
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [RequireAuth]
    public class ExamAssignmentController : ControllerBase
    {
        private readonly IExamAssignmentService _assignmentService;
        private readonly BanTayVangDbContext _context;

        public ExamAssignmentController(IExamAssignmentService assignmentService, BanTayVangDbContext context)
        {
            _assignmentService = assignmentService;
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách thí sinh được phân công cho 1 đề thi
        /// </summary>
        [HttpGet("exam/{examId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetByExam(int examId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var exam = await _context.ExamPapers.FindAsync(examId);
                if (exam == null || exam.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _assignmentService.GetAssignmentsByExamAsync(examId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đề thi mà 1 user được phân công
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetByUser(int userId)
        {
            // BUG FIX: every sibling endpoint in this controller (GetByExam, AssignUsers, Remove,
            // ExtendTime) restricts DeptManager to their own department - this one had no check at
            // all, letting a DeptManager see which exams ANY user in the whole system is assigned to.
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null || targetUser.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _assignmentService.GetAssignmentsByUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy đề thi của user hiện tại
        /// </summary>
        [HttpGet("my-exams")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetMyExams()
        {
            var userId = HttpContext.Items["UserId"] as int?;
            if (userId == null) return Unauthorized(new BaseResponseDto { Success = false, Message = "Không xác định được người dùng" });
            var result = await _assignmentService.GetAssignmentsByUserAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Phân công nhiều thí sinh cho 1 đề thi
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<int>>> AssignUsers([FromBody] CreateExamAssignmentDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return Forbid();
                }

                // Check if the exam belongs to their department
                var exam = await _context.ExamPapers.FindAsync(dto.ExamId);
                if (exam == null || exam.Department != myDepartment)
                {
                    return Forbid();
                }

                // Check if all assigned users belong to their department
                if (dto.UserIds != null && dto.UserIds.Any())
                {
                    var usersCount = await _context.Users
                        .CountAsync(u => dto.UserIds.Contains(u.Id) && u.Department == myDepartment);
                    if (usersCount != dto.UserIds.Count)
                    {
                        return BadRequest(BaseResponseDto<int>.FailureResult("Chỉ được phép phân công thí sinh thuộc khoa của mình."));
                    }
                }
            }

            var result = await _assignmentService.AssignUsersToExamAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Hủy phân công 1 thí sinh
        /// </summary>
        [HttpDelete("{assignmentId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> Remove(int assignmentId)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var assignment = await _context.ExamAssignments
                    .Include(a => a.Exam)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);
                if (assignment == null || assignment.Exam?.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _assignmentService.RemoveAssignmentAsync(assignmentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra user có được phân công cho đề thi không
        /// </summary>
        [HttpGet("check/{examId}/{userId}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<bool>>> CheckAssignment(int examId, int userId)
        {
            // BUG FIX: every sibling endpoint here (GetByExam, GetByUser, AssignUsers, Remove,
            // ExtendTime) restricts a DeptManager to their own department - this one didn't,
            // letting a DeptManager probe assignment status for any examId/userId combination
            // regardless of department.
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var exam = await _context.ExamPapers.FindAsync(examId);
                if (exam == null || exam.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _assignmentService.IsUserAssignedAsync(examId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Gia hạn thời gian làm bài cho 1 thí sinh
        /// </summary>
        [HttpPost("extend-time")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ExtendTime([FromBody] ExtendExamTimeDto dto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                var examSubmission = await _context.ExamSubmissions
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.Id == dto.ExamSubmissionId);
                if (examSubmission == null || examSubmission.User?.Department != myDepartment)
                {
                    return Forbid();
                }
            }

            var result = await _assignmentService.ExtendExamTimeAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}