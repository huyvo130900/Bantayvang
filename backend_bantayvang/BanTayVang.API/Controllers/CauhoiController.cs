using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Question management controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CauhoiController : ControllerBase
    {
        private readonly ICauhoiService _cauhoiService;
        private readonly ILogger<CauhoiController> _logger;

        public CauhoiController(ICauhoiService cauhoiService, ILogger<CauhoiController> logger)
        {
            _cauhoiService = cauhoiService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<PagedResultDto<CauhoiDto>>>> GetQuestions([FromQuery] QuestionFilterDto filter)
        {
            var result = await _cauhoiService.GetFilteredQuestionsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> GetQuestion(int id)
        {
            var result = await _cauhoiService.GetQuestionByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> CreateQuestion([FromBody] CreateCauhoiDto createDto)
        {
            var nguoiTao = GetCurrentUserIdOrDefault();
            
            var result = await _cauhoiService.CreateQuestionAsync(createDto, nguoiTao);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetQuestion), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<CauhoiDto>>> UpdateQuestion(int id, [FromBody] UpdateCauhoiDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest("ID không khớp");

            var nguoiCapNhat = GetCurrentUserIdOrDefault();
            
            var result = await _cauhoiService.UpdateQuestionAsync(updateDto, nguoiCapNhat);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseDto>> DeleteQuestion(int id)
        {
            var nguoiCapNhat = GetCurrentUserIdOrDefault();
            
            var result = await _cauhoiService.DeleteQuestionAsync(id, nguoiCapNhat);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<ActionResult<BaseResponseDto<List<CauhoiDto>>>> ImportQuestions(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ");

            var nguoiTao = GetCurrentUserIdOrDefault();
            
            var result = await _cauhoiService.ImportQuestionsFromExcelAsync(file, nguoiTao);
            return Ok(result);
        }

        /// <summary>
        /// Download template Excel để import câu hỏi
        /// Format: A=Nội dung, B=Điểm, C=Độ khó, D=IdDanhMuc, E=IdLoaiCauHoi, F-I=Lựa chọn 1-4, J=Đáp án đúng (1-4)
        /// </summary>
        [HttpGet("import-template")]
        public IActionResult DownloadImportTemplate()
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Cau Hoi Import");

            // Header
            ws.Cell(1, 1).Value = "Noi Dung Cau Hoi";
            ws.Cell(1, 2).Value = "Diem";
            ws.Cell(1, 3).Value = "Do Kho";
            ws.Cell(1, 4).Value = "Id Danh Muc";
            ws.Cell(1, 5).Value = "Id Loai Cau Hoi";
            ws.Cell(1, 6).Value = "Lua Chon 1";
            ws.Cell(1, 7).Value = "Lua Chon 2";
            ws.Cell(1, 8).Value = "Lua Chon 3";
            ws.Cell(1, 9).Value = "Lua Chon 4";
            ws.Cell(1, 10).Value = "Dap An Dung (1-4)";

            var headerRange = ws.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Sample data
            ws.Cell(2, 1).Value = "Tu khoa nao dung de khai bao class trong C#?";
            ws.Cell(2, 2).Value = 1.0;
            ws.Cell(2, 3).Value = "De";
            ws.Cell(2, 4).Value = 1;
            ws.Cell(2, 5).Value = 1;
            ws.Cell(2, 6).Value = "class";
            ws.Cell(2, 7).Value = "struct";
            ws.Cell(2, 8).Value = "interface";
            ws.Cell(2, 9).Value = "enum";
            ws.Cell(2, 10).Value = 1;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "import_cau_hoi_template.xlsx");
        }

        [HttpGet("random")]
        public async Task<ActionResult<BaseResponseDto<List<CauhoiDto>>>> GetRandomQuestions(
            [FromQuery] int count = 10, 
            [FromQuery] int? danhMucId = null)
        {
            var result = await _cauhoiService.GetRandomQuestionsAsync(count, danhMucId);
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