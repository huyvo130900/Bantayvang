using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// User session tracking for security monitoring
    /// OWASP A09: Security Logging and Monitoring
    /// </summary>
    [Table("UserSessions")]
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique session identifier
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// User ID for this session
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// When the session was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the session expires
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime? LastActivityAt { get; set; }

        /// <summary>
        /// IP address for this session
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent for this session
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether the session is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// How the session ended (logout, timeout, revoked)
        /// </summary>
        [StringLength(50)]
        public string? EndReason { get; set; }

        /// <summary>
        /// Navigation property to user
        /// </summary>
        [ForeignKey("UserId")]
        public virtual Taikhoan? User { get; set; }

        /// <summary>
        /// Check if session is valid and not expired
        /// </summary>
        public bool IsValid => IsActive && ExpiresAt > DateTime.UtcNow;
    }
}