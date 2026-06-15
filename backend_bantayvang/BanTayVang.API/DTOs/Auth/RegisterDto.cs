using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Auth
{
    /// <summary>
    /// DTO for user registration
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-100 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(255, ErrorMessage = "Họ tên tối đa 255 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Role ID (default = 3 - Student)
        /// 1 = Admin, 2 = Teacher, 3 = Student, 4 = Supervisor
        /// </summary>
        public int IdVaiTro { get; set; } = 3;

        /// <summary>
        /// Optional: Mã nhân viên
        /// </summary>
        public string? MaNhanVien { get; set; }

        /// <summary>
        /// Optional: Chức danh
        /// </summary>
        public string? ChucDanh { get; set; }

        /// <summary>
        /// Optional: Khoa/Phòng
        /// </summary>
        public string? KhoaPhong { get; set; }
    }
}