using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string MaKhoa { get; set; } = string.Empty;
        public string TenKhoa { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; }
        public int? DeptManagerId { get; set; }
        public string? TenQuanLy { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required]
        [StringLength(50)]
        public string MaKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TenKhoa { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;
    }

    public class UpdateDepartmentDto
    {
        [Required]
        [StringLength(255)]
        public string TenKhoa { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; }
    }

    public class AssignManagerDto
    {
        /// <summary>
        /// ID tài khoản có role DeptManager (ID=5)
        /// </summary>
        [Required]
        public int DeptManagerId { get; set; }
    }

    public class ExamVisibilityDto
    {
        [Required]
        public bool CongBoKetQua { get; set; }
    }

    public class DepartmentDashboardDto
    {
        public int IdKhoa { get; set; }
        public string TenKhoa { get; set; } = string.Empty;
        public int TongSoCauHoi { get; set; }
        public int TongSoDeThi { get; set; }
        public int TongSoThiSinh { get; set; }
        public double DiemTrungBinh { get; set; }
        public List<KyThiSummaryDto> KyThiGanDay { get; set; } = new();
    }

    public class KyThiSummaryDto
    {
        public int Id { get; set; }
        public string TenKyThi { get; set; } = string.Empty;
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public int SoDeThi { get; set; }
        public int SoThiSinh { get; set; }
    }
}
