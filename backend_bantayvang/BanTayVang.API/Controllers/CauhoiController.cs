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
    public class CauhoiController : ControllerBase
    {
        private readonly ICauhoiService _cauhoiService;
        private readonly ILogger<CauhoiController> _logger;
        private readonly ILoaicauhoiRepository _loaiRepository;

        public CauhoiController(
            ICauhoiService cauhoiService, 
            ILogger<CauhoiController> logger,
            ILoaicauhoiRepository loaiRepository)
        {
            _cauhoiService = cauhoiService;
            _logger = logger;
            _loaiRepository = loaiRepository;
        }

        /// <summary>
        /// GET /api/Cauhoi — Admin thấy tất cả, DeptManager chỉ thấy câu hỏi của khoa mình
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<PagedResultDto<CauhoiDto>>>> GetQuestions([FromQuery] QuestionFilterDto filter)
        {
            // DeptManager: auto-scope to their KhoaPhong
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    filter.KhoaPhong = myKhoa;
            }

            var result = await _cauhoiService.GetFilteredQuestionsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> GetQuestion(int id)
        {
            var result = await _cauhoiService.GetQuestionByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                if (!DepartmentAuthHelper.CanAccessKhoa(User, result.Data.KhoaPhong))
                    return Forbid();
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> CreateQuestion([FromBody] CreateCauhoiDto dto)
        {
            // DeptManager: auto-assign KhoaPhong
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    dto.KhoaPhong = myKhoa;
            }

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _cauhoiService.CreateQuestionAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetQuestion), new { id = result.Data?.Id }, result);
        }

        [HttpGet("check-duplicate")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<bool>>> CheckDuplicate([FromQuery] string noiDung, [FromQuery] string? khoaPhong = null, [FromQuery] int? excludeId = null)
        {
            string? targetKhoa = khoaPhong;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                    targetKhoa = myKhoa;
            }

            var exists = await _cauhoiService.CheckDuplicateAsync(noiDung, targetKhoa, excludeId);
            return Ok(new BaseResponseDto<bool>
            {
                Success = true,
                Message = exists ? "Câu hỏi đã tồn tại" : "Câu hỏi chưa tồn tại",
                Data = exists
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> UpdateQuestion(int id, [FromBody] UpdateCauhoiDto dto)
        {
            // DeptManager: check ownership
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existing = await _cauhoiService.GetQuestionByIdAsync(id);
                if (existing.Data != null && !DepartmentAuthHelper.CanAccessKhoa(User, existing.Data.KhoaPhong))
                    return Forbid();
            }

            dto.Id = id;
            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _cauhoiService.UpdateQuestionAsync(dto, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteQuestion(int id)
        {
            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _cauhoiService.DeleteQuestionAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<CauhoiDto>>>> ImportQuestions(
            IFormFile file,
            [FromForm] string khoaPhong,
            [FromForm] int idLoaiCauHoi,
            [FromForm] bool isExamImport = false,
            [FromForm] int? expectedCount = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponseDto<List<CauhoiDto>>.FailureResult("File không hợp lệ"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;

            // Bảo mật: Nếu là DeptManager, bắt buộc gán vào khoa của họ (không cho phép chọn khoa khác)
            // Nếu khoaPhong từ frontend là "Không thuộc ngân hàng", giữ nguyên để cho phép import câu hỏi tạm cho đề thi.
            string selectedKhoa = khoaPhong;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (string.IsNullOrEmpty(myKhoa))
                {
                    return BadRequest(BaseResponseDto<List<CauhoiDto>>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                if (khoaPhong != "Không thuộc ngân hàng")
                {
                    selectedKhoa = myKhoa;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedKhoa))
            {
                return BadRequest(BaseResponseDto<List<CauhoiDto>>.FailureResult("Vui lòng chọn Khoa/Phòng để import câu hỏi"));
            }

            var result = await _cauhoiService.ImportQuestionsFromExcelAsync(file, userId, selectedKhoa, idLoaiCauHoi, isExamImport, expectedCount);
            return Ok(result);
        }

        [HttpGet("random")]
        public async Task<ActionResult<BaseResponseDto<List<CauhoiDto>>>> GetRandom([FromQuery] int count = 10, [FromQuery] int? danhMucId = null)
        {
            var result = await _cauhoiService.GetRandomQuestionsAsync(count);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/Cauhoi/import-template?idLoaiCauHoi={id}
        /// Trả về file Excel template đúng theo loại câu hỏi trắc nghiệm hay tự luận
        /// </summary>
        [HttpGet("import-template")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> DownloadImportTemplate(
            [FromQuery] int idLoaiCauHoi,
            [FromQuery] bool isExamImport = false)
        {
            var result = await _cauhoiService.DownloadImportTemplateAsync(idLoaiCauHoi, isExamImport);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(BaseResponseDto.FailureResult(result.Message));
            }

            var loaiCauHoi = await _loaiRepository.GetByIdAsync(idLoaiCauHoi);
            string loaiNameNormalized = loaiCauHoi != null 
                ? (loaiCauHoi.TenLoai?.ToLower().Contains("tự luận") == true ? "tu_luan" : "trac_nghiem") 
                : "cau_hoi";

            string fileName = $"template_import_{loaiNameNormalized}.xlsx";

            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
