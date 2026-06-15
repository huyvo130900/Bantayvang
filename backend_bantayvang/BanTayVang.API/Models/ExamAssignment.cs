using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Exam assignment - assign users to specific exams
    /// OWASP A01: Access Control - only assigned users can take the exam
    /// </summary>
    [Table("ExamAssignments")]
    public class ExamAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int? AssignedBy { get; set; }

        public DateTime? CustomStartTime { get; set; }

        public int? ExtraMinutes { get; set; } // Additional time (gia hạn)

        public bool IsActive { get; set; } = true;

        public string? Note { get; set; }

        [ForeignKey("ExamId")]
        public virtual Dethi? Exam { get; set; }

        [ForeignKey("UserId")]
        public virtual Taikhoan? User { get; set; }
    }
}