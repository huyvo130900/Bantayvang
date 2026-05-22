namespace BanTayVang.API.Services.Interfaces.Auth
{
    /// <summary>
    /// Service for password hashing and verification
    /// OWASP A02: Cryptographic Failures prevention
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Hash a password using BCrypt with salt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verify a password against its hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hashedPassword">Hashed password from database</param>
        /// <returns>True if password matches</returns>
        bool VerifyPassword(string password, string hashedPassword);

        /// <summary>
        /// Generate a cryptographically secure random salt
        /// </summary>
        /// <returns>Base64 encoded salt</returns>
        string GenerateSalt();

        /// <summary>
        /// Validate password strength according to security policy
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>Validation result with errors if any</returns>
        PasswordValidationResult ValidatePasswordStrength(string password);

        /// <summary>
        /// Generate a secure random password
        /// </summary>
        /// <param name="length">Password length (minimum 12)</param>
        /// <returns>Generated password</returns>
        string GenerateSecurePassword(int length = 16);
    }

    /// <summary>
    /// Password validation result
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int StrengthScore { get; set; } // 0-100
    }
}