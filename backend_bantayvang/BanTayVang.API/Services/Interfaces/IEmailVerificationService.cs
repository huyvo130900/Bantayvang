namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Xác thực địa chỉ email bằng mã OTP 6 số gửi qua email.
    /// Dùng chung cho: xác thực email lúc thí sinh ngoại đăng ký, và luồng quên mật khẩu.
    /// </summary>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Sinh mã OTP mới, lưu (hash) vào DB và gửi email.
        /// purpose: "RegisterVerification" | "PasswordReset"
        /// Trả về false nếu bị giới hạn tần suất gửi (rate limit) hoặc lỗi gửi mail.
        /// </summary>
        Task<(bool Success, string Message)> SendCodeAsync(string email, string purpose, string? ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra mã OTP người dùng nhập. Nếu đúng, đánh dấu IsVerified = true.
        /// </summary>
        Task<(bool Success, string Message)> VerifyCodeAsync(string email, string code, string purpose, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra email đã được xác thực gần đây (IsVerified = true, chưa dùng, trong thời hạn cho phép)
        /// và đánh dấu là đã sử dụng (IsUsed = true) - dùng 1 lần duy nhất.
        /// </summary>
        Task<bool> ConsumeVerifiedCodeAsync(string email, string purpose, CancellationToken cancellationToken = default);
    }
}
