using BanTayVang.API.DTOs.Category;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        // ===== Question Types (Loai cau hoi) =====

        /// <summary>
        /// Lấy danh sách tất cả loại câu hỏi
        /// </summary>
        [HttpGet("types")]
        public async Task<ActionResult<BaseResponseDto<List<QuestionCategoryDto>>>> GetQuestionTypes()
        {
            var result = await _categoryService.GetAllQuestionTypesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết loại câu hỏi theo ID
        /// </summary>
        [HttpGet("types/{id}")]
        public async Task<ActionResult<BaseResponseDto<QuestionCategoryDto>>> GetQuestionType(int id)
        {
            var result = await _categoryService.GetQuestionTypeByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo loại câu hỏi mới
        /// </summary>
        // BUG FIX: question categories (TN/TL) are a system-wide shared taxonomy, not scoped to any
        // department, but Create/Update/Delete below were ManagementOnly - any DeptManager could
        // rename/delete/create a category used by every other department (EssayQuestionHelper
        // matches essay detection by category name across the whole app). Restrict to Admin only.
        [HttpPost("types")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto<QuestionCategoryDto>>> CreateQuestionType([FromBody] CreateQuestionCategoryDto createDto)
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto<QuestionCategoryDto>>> UpdateQuestionType(int id, [FromBody] CreateQuestionCategoryDto updateDto)
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteQuestionType(int id)
        {
            var result = await _categoryService.DeleteQuestionTypeAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
