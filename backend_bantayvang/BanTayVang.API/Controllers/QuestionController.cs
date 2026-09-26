using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Import;
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
        private readonly IWordQuestionImportService _wordImportService;

        public QuestionController(
            IQuestionService questionService,
            ILogger<QuestionController> logger,
            IQuestionCategoryRepository loaiRepository,
            IWordQuestionImportService wordImportService)
        {
            _questionService = questionService;
            _logger = logger;
            _categoryRepository = loaiRepository;
            _wordImportService = wordImportService;
        }

        /// <summary>
        /// GET /api/Question — Admin thấy tất cả, DeptManager chỉ thấy câu hỏi của khoa mình
        /// </summary>
        // BUG FIX (CRITICAL, confirmed live with a real Student account): this only had the
        // class-level [Authorize] - ANY logged-in user, including students, could call it (and
        // GetQuestion/GetRandom below) and get QuestionDto back, which includes
        // QuestionOptionDto.IsCorrect and Question.SuggestedAnswer - the actual answer key for
        // every question in the bank, across every department, including ones on exams they're
        // about to take. The real exam-taking flow never uses this controller at all (it calls
        // ExamController.GetExamQuestions, which deliberately maps to ExamChoiceDto - no
        // IsCorrect field exists on that type). Restrict to Admin/DeptManager like every other
        // question-bank management endpoint in this controller already is.
        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<PagedResultDto<QuestionDto>>>> GetQuestions([FromQuery] QuestionFilterDto filter)
        {
            // DeptManager: auto-scope to their Department
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                    filter.Department = myDepartment;
            }

            var result = await _questionService.GetFilteredQuestionsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<QuestionDto>>> GetQuestion(int id)
        {
            var result = await _questionService.GetQuestionByIdAsync(id);
            if (!result.Success) return NotFound(result);

            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null)
            {
                if (!DepartmentAuthHelper.CanAccessDepartment(User, result.Data.Department))
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
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                    dto.Department = myDepartment;
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
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                    targetKhoa = myDepartment;
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
                if (existing.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existing.Data.Department))
                    return Forbid();

                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                    dto.Department = myDepartment;
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
                if (existing.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existing.Data.Department))
                    return Forbid();
            }

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            var result = await _questionService.DeleteQuestionAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/Question/preview-excel
        /// Parse file Excel và trả về danh sách câu hỏi để FE xem trước và chỉnh sửa
        /// trước khi import thật.
        /// </summary>
        [HttpPost("preview-excel")]
        [Authorize(Policy = "ManagementOnly")]
        // BUG FIX: unlike UploadController's image endpoints ([RequestSizeLimit(10MB)]), the
        // Excel/Word import endpoints had no explicit cap - only Kestrel's ~28MB default applied.
        // A highly-compressed "zip bomb" .xlsx/.docx (both are ZIP containers under the hood) well
        // under that limit can still expand to gigabytes once ClosedXML/OpenXml parses it, so cap
        // the upload itself rather than relying on the framework default.
        [RequestSizeLimit(20_971_520)] // 20MB
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> PreviewExcel(
            IFormFile file,
            [FromForm] string department,
            [FromForm] int questionCategoryId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("File không hợp lệ"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            string selectedKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment)) selectedKhoa = myDepartment;
            }

            if (string.IsNullOrWhiteSpace(selectedKhoa))
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Vui lòng chọn Khoa/Phòng"));

            var result = await _questionService.PreviewQuestionsFromExcelAsync(file, userId, selectedKhoa, questionCategoryId);
            return Ok(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "ManagementOnly")]
        [RequestSizeLimit(20_971_520)] // 20MB - see rationale on PreviewExcel above
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
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                {
                    return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Tài khoản quản lý khoa chưa cấu hình khoa phòng quản lý."));
                }
                if (department != "Không thuộc ngân hàng")
                {
                    selectedKhoa = myDepartment;
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
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> GetRandom([FromQuery] int count = 10, [FromQuery] int? danhMucId = null)
        {
            var result = await _questionService.GetRandomQuestionsAsync(count, danhMucId);
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
            // BUG FIX: matched only the literal substring "tự luận" in the category name, but this
            // app's real essay category is stored under the short code "TL" (see EssayQuestionHelper -
            // the same mismatch already fixed for question create/update and the FE form dialog), so
            // downloading the template for a TL category always named the file "..._trac_nghiem.xlsx".
            // The template's actual CONTENT was already correct (built from CategoryName via
            // QuestionImportStrategyFactory) - only the downloaded file's name was wrong.
            string loaiNameNormalized = questionCategory != null
                ? (EssayQuestionHelper.IsEssayCategory(questionCategory.CategoryName) ? "tu_luan" : "trac_nghiem")
                : "cau_hoi";

            string fileName = $"template_import_{loaiNameNormalized}.xlsx";

            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// POST /api/Question/import-word
        /// Import câu hỏi trắc nghiệm từ file Word (.docx) theo chuẩn Azota.
        /// Đáp án đúng được nhận diện bằng cách Bôi đậm (Bold) hoặc Gạch chân (Underline) ký tự A, B, C, D.
        /// </summary>
        [HttpPost("import-word")]
        [Authorize(Policy = "ManagementOnly")]
        [RequestSizeLimit(20_971_520)] // 20MB - see rationale on PreviewExcel above
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> ImportQuestionsFromWord(
            IFormFile file,
            [FromForm] string department,
            [FromForm] int questionCategoryId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("File không hợp lệ"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;

            // Bảo mật: DeptManager chỉ được import vào khoa của họ
            string selectedKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(myDepartment))
                    return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Tài khoản chưa cấu hình khoa phòng."));
                selectedKhoa = myDepartment;
            }

            if (string.IsNullOrWhiteSpace(selectedKhoa))
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Vui lòng chọn Khoa/Phòng để import câu hỏi"));

            var result = await _questionService.ImportQuestionsFromWordAsync(file, userId, selectedKhoa, questionCategoryId);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/Question/preview-word
        /// Parse file Word và trả về danh sách câu hỏi để FE xem trước và chỉnh sửa
        /// trước khi import thật (Bước 1 trong 2-bước import).
        /// </summary>
        [HttpPost("preview-word")]
        [Authorize(Policy = "ManagementOnly")]
        [RequestSizeLimit(20_971_520)] // 20MB - see rationale on PreviewExcel above
        public async Task<ActionResult<BaseResponseDto<List<QuestionDto>>>> PreviewWord(
            IFormFile file,
            [FromForm] string department,
            [FromForm] int questionCategoryId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("File không hợp lệ"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            string selectedKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment)) selectedKhoa = myDepartment;
            }

            if (string.IsNullOrWhiteSpace(selectedKhoa))
                return BadRequest(BaseResponseDto<List<QuestionDto>>.FailureResult("Vui lòng chọn Khoa/Phòng"));

            // Dùng lại WordImportService nhưng KHÔNG lưu vào DB — chỉ parse
            var errors = new List<string>();
            var questions = await _wordImportService.ParseAndValidateAsync(file, userId, selectedKhoa, questionCategoryId, errors);

            var dtos = questions.Select(q => new QuestionDto
            {
                Id = 0,
                Content = q.Content,
                Difficulty = q.Difficulty == "3" ? "Khó" : q.Difficulty == "2" ? "Trung bình" : "Dễ",
                Department = q.Department,
                ImageUrl = q.ImageUrl,
                SuggestedAnswer = q.SuggestedAnswer,
                QuestionCategoryId = q.QuestionCategoryId,
                Options = q.QuestionOptions?.Select(o => new QuestionOptionDto
                {
                    Id = 0, Content = o.Content, IsCorrect = o.IsCorrect ?? false, OrderIndex = o.OrderIndex ?? 0
                }).ToList() ?? new()
            }).ToList();

            return Ok(BaseResponseDto<List<QuestionDto>>.SuccessResult(dtos,
                errors.Count > 0 ? $"Xem trước xong ({errors.Count} lỗi)" : "Xem trước thành công"));
        }

        /// <summary>
        /// POST /api/Question/import-from-preview
        /// Nhận danh sách câu hỏi từ FE (đã chỉnh sửa) và lưu vào DB.
        /// </summary>
        [HttpPost("import-from-preview")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ImportFromPreview(
            [FromBody] List<CreateQuestionDto> questions,
            [FromQuery] string department,
            [FromQuery] int questionCategoryId)
        {
            if (questions == null || questions.Count == 0)
                return BadRequest(BaseResponseDto.FailureResult("Danh sách câu hỏi trống"));

            var userId = DepartmentAuthHelper.GetUserId(User) ?? 0;
            string selectedKhoa = department;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDept = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDept)) selectedKhoa = myDept;
            }

            // BUG FIX: this used to call CreateQuestionAsync and increment `saved` unconditionally,
            // ignoring its actual Success/Message result. A question rejected by CreateQuestionAsync
            // (duplicate content, validation failure, etc.) was silently dropped while the response
            // still claimed every question in the batch was imported successfully. Now we track real
            // successes and surface the per-question failure messages.
            int saved = 0;
            var failMessages = new List<string>();
            foreach (var dto in questions)
            {
                dto.Department = selectedKhoa;
                dto.QuestionCategoryId = questionCategoryId;
                var result = await _questionService.CreateQuestionAsync(dto, userId);
                if (result.Success)
                {
                    saved++;
                }
                else
                {
                    var preview = dto.Content.Length > 60 ? dto.Content.Substring(0, 60) + "..." : dto.Content;
                    failMessages.Add($"\"{preview}\": {result.Message}");
                }
            }

            if (failMessages.Count > 0)
            {
                var message = $"Đã import {saved}/{questions.Count} câu hỏi vào ngân hàng. {failMessages.Count} câu bị lỗi: {string.Join("; ", failMessages)}";
                return Ok(new BaseResponseDto { Success = saved > 0, Message = message, Errors = failMessages });
            }

            return Ok(BaseResponseDto.SuccessResult($"Đã import thành công {saved} câu hỏi vào ngân hàng."));
        }


        /// <summary>
        /// GET /api/Question/import-word-template
        /// Tải xuống file Word mẫu (.docx) để hướng dẫn người dùng soạn câu hỏi đúng cú pháp.
        /// </summary>
        [HttpGet("import-word-template")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> DownloadWordTemplate()
        {
            var result = await _questionService.DownloadWordTemplateAsync();
            if (!result.Success || result.Data == null)
                return BadRequest(BaseResponseDto.FailureResult(result.Message));

            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Mau_Import_CauHoi_TrucNghiem.docx");
        }
    }
}
