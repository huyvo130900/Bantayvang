using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;
using BanTayVang.API.Helpers;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// User management controller (admin operations)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserManagementService _userService;

        public UserController(IUserManagementService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<List<UserDto>>>> GetUsers([FromQuery] UserFilterDto filter)
        {
            // DeptManager: chỉ thấy thí sinh (role=3) thuộc khoa của mình
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                filter.IdVaiTro = 3; // Student only
                var myKhoa = DepartmentAuthHelper.GetKhoaPhong(User);
                if (!string.IsNullOrEmpty(myKhoa))
                {
                    filter.KhoaPhong = myKhoa;
                }
            }
            var result = await _userService.GetAllUsersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> CreateUser([FromBody] CreateUserDto createDto)
        {
            var result = await _userService.CreateUserAsync(createDto);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetUser), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
        {
            var result = await _userService.UpdateUserAsync(id, updateDto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<BaseResponseDto>> ActivateUser(int id)
        {
            var result = await _userService.ActivateUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<BaseResponseDto>> DeactivateUser(int id)
        {
            var result = await _userService.DeactivateUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult<BaseResponseDto>> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            var result = await _userService.ResetUserPasswordAsync(id, request.NewPassword);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeleteUser(int id)
        {
            var currentUserId = DepartmentAuthHelper.GetUserId(User);
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                return BadRequest(BaseResponseDto.FailureResult("Không thể tự xóa tài khoản của chính mình"));
            }

            var result = await _userService.DeleteUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/restore")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> RestoreUser(int id)
        {
            var result = await _userService.RestoreUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}/hard")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> HardDeleteUser(int id)
        {
            var currentUserId = DepartmentAuthHelper.GetUserId(User);
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                return BadRequest(BaseResponseDto.FailureResult("Không thể tự xóa tài khoản của chính mình"));
            }

            var result = await _userService.HardDeleteUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("bulk-delete")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> BulkDeleteUsers([FromBody] List<int> ids)
        {
            var result = await _userService.BulkDeleteUsersAsync(ids);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("import-template")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            var result = await _userService.DownloadImportTemplateAsync();
            if (!result.Success || result.Data == null)
            {
                return BadRequest(BaseResponseDto.FailureResult(result.Message));
            }
            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "MauImportTaiKhoan.xlsx");
        }

        [HttpPost("import")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto<ExcelImportResultDto>>> ImportUsers(IFormFile file)
        {
            var result = await _userService.ImportUsersFromExcelAsync(file);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}