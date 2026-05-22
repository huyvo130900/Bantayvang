using BanTayVang.API.Services.Interfaces.Auth;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BanTayVang.API.Services.Impl.Auth
{
    /// <summary>
    /// Password service implementation using BCrypt
    /// OWASP A02: Cryptographic Failures prevention
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private readonly ILogger<PasswordService> _logger;

        public PasswordService(ILogger<PasswordService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            try
            {
                // Use BCrypt with work factor 12 (recommended for 2024)
                // Higher work factor = more secure but slower
                return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw new InvalidOperationException("Failed to hash password", ex);
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        public string GenerateSalt()
        {
            try
            {
                // Generate 32 bytes of random data for salt
                var saltBytes = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating salt");
                throw new InvalidOperationException("Failed to generate salt", ex);
            }
        }

        public PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult();

            if (string.IsNullOrEmpty(password))
            {
                result.Errors.Add("Mật khẩu không được để trống");
                return result;
            }

            var score = 0;

            // Length check (minimum 8, recommended 12+)
            if (password.Length < 8)
            {
                result.Errors.Add("Mật khẩu phải có ít nhất 8 ký tự");
            }
            else if (password.Length >= 12)
            {
                score += 25;
            }
            else
            {
                score += 15;
            }

            // Lowercase letters
            if (Regex.IsMatch(password, @"[a-z]"))
            {
                score += 15;
            }
            else
            {
                result.Errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái thường");
            }

            // Uppercase letters
            if (Regex.IsMatch(password, @"[A-Z]"))
            {
                score += 15;
            }
            else
            {
                result.Errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái hoa");
            }

            // Numbers
            if (Regex.IsMatch(password, @"\d"))
            {
                score += 15;
            }
            else
            {
                result.Errors.Add("Mật khẩu phải chứa ít nhất 1 chữ số");
            }

            // Special characters
            if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                score += 15;
            }
            else
            {
                result.Errors.Add("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt");
            }

            // No repeated characters
            if (!Regex.IsMatch(password, @"(.)\1{2,}"))
            {
                score += 10;
            }
            else
            {
                result.Errors.Add("Mật khẩu không được chứa quá 2 ký tự giống nhau liên tiếp");
            }

            // No common patterns
            if (!ContainsCommonPatterns(password))
            {
                score += 5;
            }
            else
            {
                result.Errors.Add("Mật khẩu không được chứa các mẫu phổ biến (123, abc, qwerty, v.v.)");
            }

            result.StrengthScore = Math.Min(score, 100);
            result.IsValid = result.Errors.Count == 0 && result.StrengthScore >= 70;

            return result;
        }

        public string GenerateSecurePassword(int length = 16)
        {
            if (length < 12)
                throw new ArgumentException("Password length must be at least 12 characters", nameof(length));

            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var allChars = lowercase + uppercase + numbers + specialChars;
            var password = new StringBuilder();

            using var rng = RandomNumberGenerator.Create();

            // Ensure at least one character from each category
            password.Append(GetRandomChar(lowercase, rng));
            password.Append(GetRandomChar(uppercase, rng));
            password.Append(GetRandomChar(numbers, rng));
            password.Append(GetRandomChar(specialChars, rng));

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // Shuffle the password
            return ShuffleString(password.ToString(), rng);
        }

        #region Private Helper Methods

        private static char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % chars.Length;
            return chars[randomIndex];
        }

        private static string ShuffleString(string input, RandomNumberGenerator rng)
        {
            var array = input.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % (i + 1);
                (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
            }
            return new string(array);
        }

        private static bool ContainsCommonPatterns(string password)
        {
            var commonPatterns = new[]
            {
                "123", "abc", "qwerty", "password", "admin", "user",
                "111", "000", "aaa", "zzz", "qwe", "asd", "zxc"
            };

            var lowerPassword = password.ToLowerInvariant();
            return commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
        }

        #endregion
    }
}