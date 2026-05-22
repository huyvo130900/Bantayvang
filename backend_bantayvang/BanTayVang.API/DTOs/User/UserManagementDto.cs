using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.User
{
    /// <summary>
    /// User information DTO for management
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string? MaNhanVien { get; set; }
        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
        public string? HoTen { get; set; }
        public string? ChucDanh { get; set; }
        public string? KhoaPhong { get; set; }
        public int? IdVaiTro { get; set; }
        public string? TenVaiTro { get; set; }
        public bool? TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
    }

    /// <summary>
    /// DTO for creating user by admin
    /// </summary>
    public class CreateUserDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string MatKhau { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string HoTen { get; set; } = string.Empty;

        public string? MaNhanVien { get; set; }
        public string? ChucDanh { get; set; }
        public string? KhoaPhong { get; set; }
        public int IdVaiTro { get; set; } = 3;
        public bool TrangThai { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating user by admin
    /// </summary>
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string HoTen { get; set; } = string.Empty;

        public string? MaNhanVien { get; set; }
        public string? ChucDanh { get; set; }
        public string? KhoaPhong { get; set; }
        public int IdVaiTro { get; set; }
        public bool TrangThai { get; set; }
    }

    /// <summary>
    /// DTO for filtering users
    /// </summary>
    public class UserFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? IdVaiTro { get; set; }
        public bool? TrangThai { get; set; }
        public string? KhoaPhong { get; set; }
        public string? SearchKeyword { get; set; }
    }
}