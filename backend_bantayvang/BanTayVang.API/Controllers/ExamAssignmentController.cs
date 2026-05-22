using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Exam assignment controller - admin assigns users to exams & extends time
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExamAssignmentController : ControllerBase
    {
        private readonly IExamAssignmentService _assignmentService;

        public ExamAssignmentController(IExamAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        /// <summary>
        /// Lấy danh sách thí sinh được phân công cho 1 đề thi
        /// </summary>
        [HttpGet("exam/{examId}")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetByExam(int examId)
        {
            var result = await _assignmentService.GetAssignmentsByExamAsync(examId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đề thi mà 1 user được phân công
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetByUser(int userId)
        {
            var result = await _assignmentService.GetAssignmentsByUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy đề thi của user hiện tại
        /// </summary>
        [HttpGet("my-exams")]
        public async Task<ActionResult<BaseResponseDto<List<ExamAssignmentDto>>>> GetMyExams()
        {
            var userId = HttpContext.Items["UserId"] as int? ?? 1;
            var result = await _assignmentService.GetAssignmentsByUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Phân công nhiều thí sinh cho 1 đề thi
        /// </summary>
        [HttpPost("assign")]
        public async Task<ActionResult<BaseResponseDto<int>>> AssignUsers([FromBody] CreateExamAssignmentDto dto)
        {
            var result = await _assignmentService.AssignUsersToExamAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Hủy phân công 1 thí sinh
        /// </summary>
        [HttpDelete("{assignmentId}")]
        public async Task<ActionResult<BaseResponseDto>> Remove(int assignmentId)
        {
            var result = await _assignmentService.RemoveAssignmentAsync(assignmentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra user có được phân công cho đề thi không
        /// </summary>
        [HttpGet("check/{examId}/{userId}")]
        public async Task<ActionResult<BaseResponseDto<bool>>> CheckAssignment(int examId, int userId)
        {
            var result = await _assignmentService.IsUserAssignedAsync(examId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Gia hạn thời gian làm bài cho 1 thí sinh
        /// </summary>
        [HttpPost("extend-time")]
        public async Task<ActionResult<BaseResponseDto>> ExtendTime([FromBody] ExtendExamTimeDto dto)
        {
            var result = await _assignmentService.ExtendExamTimeAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}