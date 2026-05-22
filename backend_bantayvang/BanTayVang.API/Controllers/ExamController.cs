using BanTayVang.API.DTOs.AntiCheat;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Exam controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly ILogger<ExamController> _logger;

        public ExamController(IExamService examService, ILogger<ExamController> logger)
        {
            _examService = examService;
            _logger = logger;
        }

        [HttpGet("active")]
        public async Task<ActionResult<BaseResponseDto<List<DethiDto>>>> GetActiveExams()
        {
            var result = await _examService.GetActiveExamsAsync();
            return Ok(result);
        }

        [HttpGet("code/{maDeThi}")]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> GetExamByCode(string maDeThi)
        {
            var result = await _examService.GetExamByCodeAsync(maDeThi);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<DethiDto>>> CreateExam([FromBody] CreateDethiDto createDto)
        {
            var nguoiTao = GetCurrentUserIdOrDefault();
            
            var result = await _examService.CreateExamAsync(createDto, nguoiTao);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetExamByCode), new { maDeThi = createDto.MaDeThi }, result);
        }

        [HttpPost("start")]
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> StartExam([FromBody] StartExamDto startDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            var result = await _examService.StartExamAsync(startDto, taikhoanId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{baithiId}/questions")]
        public async Task<ActionResult<BaseResponseDto<List<ExamQuestionDto>>>> GetExamQuestions(int baithiId)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            var result = await _examService.GetExamQuestionsAsync(baithiId, taikhoanId);
            return Ok(result);
        }

        [HttpPost("answer")]
        public async Task<ActionResult<BaseResponseDto>> SaveAnswer([FromBody] SubmitAnswerDto answerDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            var result = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
            return Ok(result);
        }

        /// <summary>
        /// Lưu nhiều đáp án cho 1 câu hỏi (Multiple correct answers)
        /// </summary>
        [HttpPost("answer-multiple")]
        public async Task<ActionResult<BaseResponseDto>> SaveMultipleAnswer([FromBody] SubmitMultipleAnswerDto dto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            // Save each choice as a separate answer
            BaseResponseDto? lastResult = null;
            
            if (dto.IdLuaChonDaChon == null || !dto.IdLuaChonDaChon.Any())
            {
                // No choices - save as text answer or empty
                var answerDto = new SubmitAnswerDto
                {
                    IdBaiThi = dto.IdBaiThi,
                    IdCauHoi = dto.IdCauHoi,
                    IdLuaChonDaChon = null,
                    CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                    DaLuu = dto.DaLuu
                };
                lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
            }
            else
            {
                foreach (var choiceId in dto.IdLuaChonDaChon)
                {
                    var answerDto = new SubmitAnswerDto
                    {
                        IdBaiThi = dto.IdBaiThi,
                        IdCauHoi = dto.IdCauHoi,
                        IdLuaChonDaChon = choiceId,
                        CauTraLoiTuLuan = dto.CauTraLoiTuLuan,
                        DaLuu = dto.DaLuu
                    };
                    lastResult = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
                }
            }

            return Ok(lastResult ?? new BaseResponseDto { Success = true, Message = "Lưu đáp án" });
        }

        [HttpGet("{baithiId}/progress")]
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> GetExamProgress(int baithiId)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            var result = await _examService.GetExamProgressAsync(baithiId, taikhoanId);
            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<BaseResponseDto<BaithiDto>>> SubmitExam([FromBody] SubmitExamDto submitDto)
        {
            var taikhoanId = GetCurrentUserIdOrDefault();
            
            var result = await _examService.SubmitExamAsync(submitDto, taikhoanId);
            return Ok(result);
        }

        [HttpPost("warning")]
        public async Task<ActionResult<BaseResponseDto>> LogCheatingWarning([FromBody] CheatingWarningDto warningDto)
        {
            var result = await _examService.LogSuspiciousActivityAsync(
                warningDto.IdBaiThi, 
                warningDto.LoaiCanhBao, 
                warningDto.MoTa ?? "");
            return Ok(result);
        }

        [HttpGet("{baithiId}/warnings")]
        public async Task<ActionResult<BaseResponseDto<int>>> GetWarningCount(int baithiId)
        {
            var result = await _examService.GetWarningCountAsync(baithiId);
            return Ok(result);
        }

        /// <summary>
        /// Get current user ID from JWT context, default to 1 (admin) if not authenticated
        /// </summary>
        private int GetCurrentUserIdOrDefault()
        {
            return HttpContext.Items["UserId"] as int? ?? 1;
        }
    }
}