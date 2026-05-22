using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BanTayVang.API.Controllers
{
    /// <summary>
    /// Seed Controller - For development only
    /// Use this to reset admin password and seed initial data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly ITaikhoanRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<SeedController> _logger;
        private readonly IWebHostEnvironment _env;

        public SeedController(
            ITaikhoanRepository userRepository,
            IPasswordService passwordService,
            ILogger<SeedController> logger,
            IWebHostEnvironment env)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Reset admin password to admin123
        /// Only available in Development environment
        /// </summary>
        [HttpPost("reset-admin-password")]
        public async Task<IActionResult> ResetAdminPassword()
        {
            // Only allow in development
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { success = false, message = "This endpoint is only available in Development environment" });
            }

            try
            {
                var admin = await _userRepository.GetByUsernameOrEmailAsync("admin");
                if (admin == null)
                {
                    return NotFound(new { success = false, message = "Admin user not found" });
                }

                // Generate new hash for "admin123"
                var newHash = _passwordService.HashPassword("admin123");
                
                admin.MatKhau = newHash;
                admin.TrangThai = true;
                admin.IdVaiTro = 1;
                if (string.IsNullOrEmpty(admin.Email))
                    admin.Email = "admin@bantayvang.vn";
                if (string.IsNullOrEmpty(admin.HoTen))
                    admin.HoTen = "Quản trị viên hệ thống";

                await _userRepository.UpdateAsync(admin);

                _logger.LogInformation("Admin password reset successfully");

                return Ok(new 
                { 
                    success = true, 
                    message = "Admin password reset successfully",
                    credentials = new
                    {
                        username = "admin",
                        password = "admin123"
                    },
                    hashGenerated = newHash
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting admin password");
                return StatusCode(500, new { success = false, message = "Error resetting admin password", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate password hash for a given password
        /// Only available in Development environment
        /// </summary>
        [HttpGet("generate-hash/{password}")]
        public IActionResult GenerateHash(string password)
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { success = false, message = "This endpoint is only available in Development environment" });
            }

            var hash = _passwordService.HashPassword(password);
            return Ok(new 
            { 
                success = true, 
                password = password,
                hash = hash 
            });
        }
    }
}