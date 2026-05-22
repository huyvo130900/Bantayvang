using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Auth
{
    /// <summary>
    /// DTO for refresh token request
    /// </summary>
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token không được để trống")]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Client IP address for security validation
        /// </summary>
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// DTO for change password request
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8-100 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa, 1 số và 1 ký tự đặc biệt")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}