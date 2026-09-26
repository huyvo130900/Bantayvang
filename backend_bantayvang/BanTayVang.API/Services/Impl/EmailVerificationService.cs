using System.Security.Cryptography;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly BanTayVangDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerificationService> _logger;

        private const int CodeLength = 6;
        private const int CodeExpiryMinutes = 10;
        private const int ResendCooldownSeconds = 60;
        private const int MaxFailedAttempts = 5;
        // Sau khi verify, người dùng có tối đa 30 phút để "tiêu thụ" mã (nộp đơn / đổi mật khẩu)
        private const int ConsumeWindowMinutes = 30;

        public EmailVerificationService(
            BanTayVangDbContext context,
            IEmailService emailService,
            ILogger<EmailVerificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SendCodeAsync(string email, string purpose, string? ipAddress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email không được để trống");
            }

            email = email.Trim().ToLowerInvariant();

            // Chống spam: chỉ cho gửi lại sau ResendCooldownSeconds
            var recentCode = await _context.EmailVerificationCodes
                .Where(c => c.Email == email && c.Purpose == purpose)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (recentCode != null && recentCode.CreatedAt.AddSeconds(ResendCooldownSeconds) > DateTime.UtcNow.AddHours(7))
            {
                var waitSeconds = (int)(recentCode.CreatedAt.AddSeconds(ResendCooldownSeconds) - DateTime.UtcNow.AddHours(7)).TotalSeconds;
                return (false, $"Vui lòng đợi {waitSeconds} giây trước khi yêu cầu gửi lại mã.");
            }

            var code = GenerateNumericCode(CodeLength);
            var codeHash = BCrypt.Net.BCrypt.HashPassword(code, workFactor: 10);

            var entity = new EmailVerificationCode
            {
                Email = email,
                CodeHash = codeHash,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddHours(7).AddMinutes(CodeExpiryMinutes),
                CreatedAt = DateTime.UtcNow.AddHours(7),
                IpAddress = ipAddress
            };

            _context.EmailVerificationCodes.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            var sent = await _emailService.SendVerificationCodeEmailAsync(email, code, purpose);
            if (!sent)
            {
                _logger.LogWarning("Failed to send verification code email to {Email}", email);
                return (false, "Không thể gửi email. Vui lòng thử lại sau.");
            }

            return (true, "Mã xác nhận đã được gửi tới email của bạn.");
        }

        public async Task<(bool Success, string Message)> VerifyCodeAsync(string email, string code, string purpose, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return (false, "Vui lòng nhập đầy đủ email và mã xác nhận");
            }

            email = email.Trim().ToLowerInvariant();

            var entity = await _context.EmailVerificationCodes
                .Where(c => c.Email == email && c.Purpose == purpose && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                return (false, "Không tìm thấy yêu cầu xác thực. Vui lòng gửi lại mã.");
            }

            if (entity.ExpiresAt < DateTime.UtcNow.AddHours(7))
            {
                return (false, "Mã xác nhận đã hết hạn. Vui lòng gửi lại mã.");
            }

            if (entity.FailedAttempts >= MaxFailedAttempts)
            {
                return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã mới.");
            }

            if (!BCrypt.Net.BCrypt.Verify(code, entity.CodeHash))
            {
                // BUG FIX: read-modify-write (`entity.FailedAttempts += 1` then SaveChanges) is not
                // atomic - firing several guesses against the same code in parallel lets each
                // request read the same stale FailedAttempts value before any of them save, so the
                // counter effectively gets stuck near 1 instead of accumulating, defeating the
                // MaxFailedAttempts brute-force lockout entirely for anyone guessing concurrently.
                // Same class of bug already fixed elsewhere in this codebase (e.g. WarningCount) -
                // ExecuteUpdateAsync translates to an atomic "SET FailedAttempts = FailedAttempts + 1".
                await _context.EmailVerificationCodes
                    .Where(c => c.Id == entity.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.FailedAttempts, c => c.FailedAttempts + 1), cancellationToken);
                return (false, "Mã xác nhận không đúng.");
            }

            entity.IsVerified = true;
            entity.VerifiedAt = DateTime.UtcNow.AddHours(7);
            await _context.SaveChangesAsync(cancellationToken);

            return (true, "Xác thực email thành công.");
        }

        public async Task<bool> ConsumeVerifiedCodeAsync(string email, string purpose, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            email = email.Trim().ToLowerInvariant();

            var entity = await _context.EmailVerificationCodes
                .Where(c => c.Email == email
                    && c.Purpose == purpose
                    && c.IsVerified
                    && !c.IsUsed
                    && c.VerifiedAt != null
                    && c.VerifiedAt.Value.AddMinutes(ConsumeWindowMinutes) > DateTime.UtcNow.AddHours(7))
                .OrderByDescending(c => c.VerifiedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null) return false;

            entity.IsUsed = true;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string GenerateNumericCode(int length)
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var value = Math.Abs(BitConverter.ToInt32(bytes, 0));
            var max = (int)Math.Pow(10, length);
            var code = value % max;
            return code.ToString(new string('0', length));
        }
    }
}
