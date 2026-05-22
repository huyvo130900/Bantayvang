using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.User;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// User management controller (admin operations)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserManagementService _userService;

        public UserController(IUserManagementService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<List<UserDto>>>> GetUsers([FromQuery] UserFilterDto filter)
        {
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
        public async Task<ActionResult<BaseResponseDto>> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}