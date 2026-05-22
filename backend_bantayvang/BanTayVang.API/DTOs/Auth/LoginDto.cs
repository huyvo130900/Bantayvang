using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Auth
{
    /// <summary>
    /// DTO for user login request
    /// OWASP A03: Input validation to prevent injection attacks
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9._@-]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và các ký tự ._@-")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Remember me option for extended session
        /// </summary>
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// Client IP address for security logging
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent for security tracking
        /// </summary>
        public string? UserAgent { get; set; }
    }
}