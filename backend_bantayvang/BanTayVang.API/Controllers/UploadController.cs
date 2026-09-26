using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// File upload controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IFileUploadService _uploadService;

        public UploadController(IFileUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        /// <summary>
        /// Upload image cho câu hỏi (chỉ dành cho admin/quản lý)
        /// </summary>
        [HttpPost("image")]
        [Authorize(Policy = "ManagementOnly")]
        [RequestSizeLimit(10_485_760)] // 10MB
        public async Task<ActionResult<BaseResponseDto<FileUploadResult>>> UploadImage(IFormFile file, [FromQuery] string folder = "questions")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponseDto<FileUploadResult> { Success = false, Message = "Không có file" });

            var result = await _uploadService.UploadImageAsync(file, folder);
            
            if (!result.Success)
                return BadRequest(new BaseResponseDto<FileUploadResult> { Success = false, Message = result.Message });

            return Ok(new BaseResponseDto<FileUploadResult>
            {
                Success = true,
                Message = result.Message ?? "Upload thành công",
                Data = result
            });
        }

        /// <summary>
        /// Upload ảnh đính kèm câu trả lời tự luận (dành cho tất cả user đã đăng nhập)
        /// Ảnh sẽ lưu vào thư mục essays/ để phân tách với ảnh câu hỏi
        /// </summary>
        [HttpPost("essay-image")]
        [RequestSizeLimit(10_485_760)] // 10MB
        public async Task<ActionResult<BaseResponseDto<FileUploadResult>>> UploadEssayImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponseDto<FileUploadResult> { Success = false, Message = "Không có file" });

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new BaseResponseDto<FileUploadResult> { Success = false, Message = "Chỉ chấp nhận file ảnh (JPG, PNG, GIF, BMP, WebP)" });

            var result = await _uploadService.UploadImageAsync(file, "essays");
            
            if (!result.Success)
                return BadRequest(new BaseResponseDto<FileUploadResult> { Success = false, Message = result.Message });

            return Ok(new BaseResponseDto<FileUploadResult>
            {
                Success = true,
                Message = result.Message ?? "Upload thành công",
                Data = result
            });
        }

        /// <summary>
        /// Xóa file đã upload (chỉ Admin)
        /// </summary>
        // BUG FIX: uploaded files carry no department/ownership metadata at all (just a folder
        // slug), so there was no way for a DeptManager-scoped check here to mean anything - yet
        // this was ManagementOnly, letting any DeptManager delete any file (question/exam images
        // of OTHER departments) as long as they knew or could guess the URL. Nothing in the
        // frontend ever calls this endpoint anyway (grepped - only image upload is used), so
        // restricting to Admin only costs no real functionality.
        [HttpDelete]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteFile([FromQuery] string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest(new BaseResponseDto { Success = false, Message = "Thiếu fileUrl" });

            var deleted = await _uploadService.DeleteFileAsync(fileUrl);
            
            return Ok(new BaseResponseDto
            {
                Success = deleted,
                Message = deleted ? "Đã xóa file" : "Không tìm thấy file hoặc lỗi xóa"
            });
        }
    }
}