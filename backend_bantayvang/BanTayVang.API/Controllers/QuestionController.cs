using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        private readonly ILogger<QuestionController> _logger;
        private readonly IQuestionCategoryRepository _categoryRepository;

        public QuestionController(
            IQuestionService cauhoiService, 
            ILogger<QuestionController> logger,
            IQuestionCategoryRepository loaiRepository)
        {
            _questionService = cauhoiService;
            _logger = logger;
            _categoryRepository = loaiRepository;
        }

        /// <summary>
        /// GET /api/Question — Admin thấy tất cả, DeptManager chỉ thấy câu hỏi của khoa mình
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<PagedResultDto<QuestionDto>>>> GetQuestions([FromQuery] QuestionFilterDto filter)
        {
            // DeptManager: auto-scope to their Department
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    filter.Department = myKhoa;
            }

            var result = await _questionService.GetFilteredQuestionsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<QuestionDto>>> GetQuestion(int id)
        {
            var result = await _questionService.GetQuestionByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                if (!DepartmentAuthHelper.CanAccessKhoa(User, result.Data.Department))
                    return Forbid();
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<QuestionDto>>> CreateQuestion([FromBody] CreateQuestionDto dto)
        {
            // DeptManager: auto-assign Department
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    dto.Department = myKhoa;
            }

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _questionService.CreateQuestionAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetQuestion), new { id = result.Data?.Id }, result);
        }

        [HttpGet("check-duplicate")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<bool>>> CheckDuplicate([FromQuery] string content, [FromQuery] string? department = null, [FromQuery] int? excludeId = null)
        {
            string? targetKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    targetKhoa = myKhoa;
            }

            var exists = await _questionService.CheckDuplicateAsync(content, targetKhoa, excludeId);
            return Ok(new BaseResponseDto<bool>
            {
                Success = true,
                Message = exists ? "Câu hỏi đã tồn tại" : "Câu hỏi chưa tồn tại",
                Data = exists
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<QuestionDto>>> UpdateQuestion(int id, [FromBody] UpdateQuestionDto dto)
        {
            // DeptManager: check ownership
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existing = await _questionService.GetQuestionByIdAsync(id);
                if (existing.Data != null && !DepartmentAuthHelper.CanAccessKhoa(User, existing.Data.Department))
                    return Forbid();
            }

            dto.Id = id;
            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _questionService.UpdateQuestionAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteQuestion(int id)
        {
            // DeptManager: check ownership
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existing = await _questionService.GetQuestionByIdAsync(id);
                if (existing.Data != null && !DepartmentAuthHelper.CanAccessKhoa(User, existing.Data.Department))
                    return Forbid();
            }

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _questionService.DeleteQuestionAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> ImportQuestions(
            IFormFile file,
            [FromForm] string department,
            [FromForm] int questionCategoryId,
            [FromForm] bool isExamImport = false,
            [FromForm] int? expectedCount = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("File không hợp lệ"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;

            // Bảo mật: Nếu là DeptManager, bắt buộc gán vào khoa của họ (không cho phép chọn khoa khác)
            // Nếu department từ frontend là "Không thuộc ngân hàng", giữ nguyên để cho phép import câu hỏi tạm cho đề thi.
            string selectedKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                if (department != "Không thuộc ngân hàng")
                {
                    selectedKhoa = myKhoa;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedKhoa))
            {
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Vui lòng chọn Khoa/Phòng để import câu hỏi"));
            }

            var result = await _questionService.ImportQuestionsFromExcelAsync(file, userId, selectedKhoa, questionCategoryId, isExamImport, expectedCount);
            return Ok(result);
        }

        [HttpGet("random")]
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> GetRandom([FromQuery] int count = 10, [FromQuery] int? danhMucId = null)
        {
            var result = await _questionService.GetRandomQuestionsAsync(count);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/Question/import-template?questionCategoryId={id}
        /// Trả về file Excel template đúng theo loại câu hỏi trắc nghiệm hay tự luận
        /// </summary>
        [HttpGet("import-template")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> DownloadImportTemplate(
            [FromQuery] int questionCategoryId,
            [FromQuery] bool isExamImport = false)
        {
            var result = await _questionService.DownloadImportTemplateAsync(questionCategoryId, isExamImport);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(BaseResponseDto.FailureResult(result.Message));
            }

            var questionCategory = await _categoryRepository.GetByIdAsync(questionCategoryId);
            string loaiNameNormalized = questionCategory != null 
                ? (questionCategory.CategoryName?.ToLower().Contains("tự luận") == true ? "tu_luan" : "trac_nghiem") 
                : "cau_hoi";

            string fileName = $"template_import_{loaiNameNormalized}.xlsx";

            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
