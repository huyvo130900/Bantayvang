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
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<List<UserDto>>>> GetUsers([FromQuery] UserFilterDto filter)
        {
            // DeptManager: chỉ thấy thí sinh (role=3) thuộc khoa của mình
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                filter.RoleId = 3; // Student only
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                {
                    filter.Department = myDepartment;
                }
            }
            var result = await _userService.GetAllUsersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (!result.Success)
                return NotFound(result);

            // BUG FIX: every other per-user action here (Update/Activate/Deactivate/ResetPassword)
            // restricts a DeptManager to users in their own department via CanAccessDepartment,
            // but this plain "view user" endpoint had no such check - confirmed live: a DeptManager
            // could fetch the full profile (employee code, phone, email, role) of ANY user id,
            // including other departments' staff and Admin accounts, just by iterating ids.
            if (DepartmentAuthHelper.IsDeptManager(User) && result.Data != null &&
                !DepartmentAuthHelper.CanAccessDepartment(User, result.Data.Department))
            {
                return Forbid();
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> CreateUser([FromBody] CreateUserDto createDto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                {
                    createDto.Department = myDepartment;
                }
                if (createDto.RoleId != 3) // DeptManager can only create students
                {
                    return Forbid();
                }
            }

            var result = await _userService.CreateUserAsync(createDto);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetUser), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existingUser.Data.Department))
                {
                    return Forbid();
                }
                
                var myDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (!string.IsNullOrEmpty(myDepartment))
                {
                    updateDto.Department = myDepartment;
                }

                if (updateDto.RoleId != 3) // DeptManager can only manage students
                {
                    return Forbid();
                }
            }

            var result = await _userService.UpdateUserAsync(id, updateDto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/activate")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ActivateUser(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existingUser.Data.Department))
                    return Forbid();
            }

            var result = await _userService.ActivateUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> DeactivateUser(int id)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existingUser.Data.Department))
                    return Forbid();
            }

            var result = await _userService.DeactivateUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<ActionResult<BaseResponseDto>> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser.Data != null && !DepartmentAuthHelper.CanAccessDepartment(User, existingUser.Data.Department))
                    return Forbid();
            }

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
            // BUG FIX (IDOR, confirmed live): this endpoint is ManagementOnly (Admin + DeptManager)
            // but the service applied zero department scoping - a DeptManager could pass any user
            // ids at all and soft-delete/disable them, including accounts from other departments.
            // Every sibling per-user action (Update/Activate/Deactivate/ResetPassword/GetUser)
            // restricts DeptManager to their own department; this bulk variant must too.
            string? restrictToDepartment = null;
            if (DepartmentAuthHelper.IsDeptManager(User))
            {
                restrictToDepartment = DepartmentAuthHelper.GetDepartmentClaim(User);
                if (string.IsNullOrEmpty(restrictToDepartment))
                    return Forbid();
            }

            // BUG FIX: unlike BulkHardDeleteUsers/DeleteUser/HardDeleteUser, this had no
            // self-delete guard - confirmed live (accidentally, during testing) that a caller
            // could disable their own account by including their own id in the list.
            var currentUserId = DepartmentAuthHelper.GetUserId(User);
            if (currentUserId.HasValue && ids.Contains(currentUserId.Value))
            {
                return BadRequest(BaseResponseDto.FailureResult("Không thể tự xóa tài khoản của chính mình trong danh sách"));
            }

            var result = await _userService.BulkDeleteUsersAsync(ids, restrictToDepartment);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("bulk-hard-delete")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<BaseResponseDto>> BulkHardDeleteUsers([FromBody] List<int> ids)
        {
            // Similar to HardDeleteUser, we need to make sure the user isn't trying to hard delete themselves
            var currentUserId = DepartmentAuthHelper.GetUserId(User);
            if (currentUserId.HasValue && ids.Contains(currentUserId.Value))
            {
                return BadRequest(BaseResponseDto.FailureResult("Không thể tự xóa vĩnh viễn tài khoản của chính mình trong danh sách"));
            }

            var result = await _userService.BulkHardDeleteUsersAsync(ids);
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
                "Template_Import_Users.xlsx");
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
