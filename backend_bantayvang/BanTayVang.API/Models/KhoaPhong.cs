using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Khoa/Phòng ban - quản lý phân quyền theo khoa
    /// </summary>
    [Table("KHOA_PHONG")]
    public class KhoaPhong
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MaKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TenKhoa { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;

        /// <summary>
        /// FK -> TAIKHOAN.Id - người phụ trách khoa
        /// </summary>
        public int? DeptManagerId { get; set; }

        public int? NguoiTao { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        [ForeignKey("DeptManagerId")]
        public virtual Taikhoan? DeptManager { get; set; }
    }
}
