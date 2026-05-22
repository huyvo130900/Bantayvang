namespace BanTayVang.API.DTOs.Auth
{
    /// <summary>
    /// DTO for authentication response
    /// Contains JWT token and user information
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// JWT access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for token renewal
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Token type (Bearer)
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// User information
        /// </summary>
        public UserInfoDto User { get; set; } = new();
    }

    /// <summary>
    /// User information DTO for authentication response
    /// </summary>
    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastLoginAt { get; set; }
    }
}