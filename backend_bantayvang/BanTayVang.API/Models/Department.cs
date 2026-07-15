using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Khoa/Phòng ban - quản lý phân quyền theo khoa
    /// </summary>
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MaKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool Status { get; set; } = true;

        /// <summary>
        /// FK -> USER.Id - người phụ trách khoa
        /// </summary>
        public int? DeptManagerId { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("DeptManagerId")]
        public virtual User? DeptManager { get; set; }
    }
}
