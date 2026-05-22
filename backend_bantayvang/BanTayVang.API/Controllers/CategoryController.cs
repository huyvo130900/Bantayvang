using BanTayVang.API.DTOs.Category;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Controller for managing question categories and types
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // ===== Categories (Danh muc) =====

        /// <summary>
        /// Lấy danh sách tất cả danh mục câu hỏi
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<BaseResponseDto<List<DanhmucauhoiDto>>>> GetCategories()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết danh mục theo ID
        /// </summary>
        [HttpGet("categories/{id}")]
        public async Task<ActionResult<BaseResponseDto<DanhmucauhoiDto>>> GetCategory(int id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo danh mục mới
        /// </summary>
        [HttpPost("categories")]
        public async Task<ActionResult<BaseResponseDto<DanhmucauhoiDto>>> CreateCategory([FromBody] CreateDanhmucauhoiDto createDto)
        {
            var result = await _categoryService.CreateCategoryAsync(createDto);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetCategory), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Cập nhật danh mục
        /// </summary>
        [HttpPut("categories/{id}")]
        public async Task<ActionResult<BaseResponseDto<DanhmucauhoiDto>>> UpdateCategory(int id, [FromBody] CreateDanhmucauhoiDto updateDto)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, updateDto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa danh mục
        /// </summary>
        [HttpDelete("categories/{id}")]
        public async Task<ActionResult<BaseResponseDto>> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // ===== Question Types (Loai cau hoi) =====

        /// <summary>
        /// Lấy danh sách tất cả loại câu hỏi
        /// </summary>
        [HttpGet("types")]
        public async Task<ActionResult<BaseResponseDto<List<LoaicauhoiDto>>>> GetQuestionTypes()
        {
            var result = await _categoryService.GetAllQuestionTypesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết loại câu hỏi theo ID
        /// </summary>
        [HttpGet("types/{id}")]
        public async Task<ActionResult<BaseResponseDto<LoaicauhoiDto>>> GetQuestionType(int id)
        {
            var result = await _categoryService.GetQuestionTypeByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo loại câu hỏi mới
        /// </summary>
        [HttpPost("types")]
        public async Task<ActionResult<BaseResponseDto<LoaicauhoiDto>>> CreateQuestionType([FromBody] CreateLoaicauhoiDto createDto)
        {
            var result = await _categoryService.CreateQuestionTypeAsync(createDto);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetQuestionType), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Cập nhật loại câu hỏi
        /// </summary>
        [HttpPut("types/{id}")]
        public async Task<ActionResult<BaseResponseDto<LoaicauhoiDto>>> UpdateQuestionType(int id, [FromBody] CreateLoaicauhoiDto updateDto)
        {
            var result = await _categoryService.UpdateQuestionTypeAsync(id, updateDto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa loại câu hỏi
        /// </summary>
        [HttpDelete("types/{id}")]
        public async Task<ActionResult<BaseResponseDto>> DeleteQuestionType(int id)
        {
            var result = await _categoryService.DeleteQuestionTypeAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}