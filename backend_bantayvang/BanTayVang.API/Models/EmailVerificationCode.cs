using System;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Mã xác nhận (OTP) gửi qua email, dùng cho:
    /// - RegisterVerification: xác thực email khi thí sinh ngoại nộp đơn đăng ký thi
    /// - PasswordReset: xác thực khi người dùng quên mật khẩu
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    public partial class EmailVerificationCode
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Mã OTP đã hash (không lưu plaintext)
        /// </summary>
        public string CodeHash { get; set; } = string.Empty;

        /// <summary>
        /// "RegisterVerification" | "PasswordReset"
        /// </summary>
        public string Purpose { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Đã nhập đúng mã (nhưng có thể chưa được "tiêu thụ" bởi bước tiếp theo)
        /// </summary>
        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Đã được sử dụng để hoàn tất hành động cuối (nộp đơn / đổi mật khẩu)
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Số lần nhập sai, chặn brute-force
        /// </summary>
        public int FailedAttempts { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public string? IpAddress { get; set; }
    }
}

