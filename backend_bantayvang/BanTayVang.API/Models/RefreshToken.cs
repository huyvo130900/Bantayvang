using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Refresh token entity for JWT token management
    /// OWASP A07: Identification and Authentication Failures prevention
    /// </summary>
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The refresh token value (hashed)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// User ID this token belongs to
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// When the token was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the token expires
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether the token has been used
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Whether the token has been revoked
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// IP address where token was created
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent where token was created
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Navigation property to user
        /// </summary>
        [ForeignKey("UserId")]
        public virtual Taikhoan? User { get; set; }

        /// <summary>
        /// Check if token is valid (not expired, used, or revoked)
        /// </summary>
        public bool IsValid => !IsUsed && !IsRevoked && ExpiresAt > DateTime.UtcNow;
    }
}