using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Notification entity
    /// </summary>
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "Info";

        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [StringLength(500)]
        public string? RelatedUrl { get; set; }

        [ForeignKey("UserId")]
        public virtual Taikhoan? User { get; set; }
    }
}