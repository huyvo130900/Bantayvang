using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// File upload controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IFileUploadService _uploadService;

        public UploadController(IFileUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        /// <summary>
        /// Upload image cho câu hỏi
        /// </summary>
        [HttpPost("image")]
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
        /// Xóa file đã upload
        /// </summary>
        [HttpDelete]
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