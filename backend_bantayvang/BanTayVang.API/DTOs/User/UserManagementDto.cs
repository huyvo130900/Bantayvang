using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.User
{
    /// <summary>
    /// User information DTO for management
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public int? DeptManagerDeptId { get; set; }
        public string? DeptManagerDeptName { get; set; }
        public bool? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// DTO for creating user by admin
    /// </summary>
    public class CreateUserDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        public string? EmployeeCode { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int RoleId { get; set; } = 3;
        /// <summary>Bắt buộc khi RoleId = 5 (DeptManager)</summary>
        public int? DeptManagerDeptId { get; set; }
        public bool Status { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating user by admin
    /// </summary>
    public class UpdateUserDto
    {
        [Required]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        public string? EmployeeCode { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public int? DeptManagerDeptId { get; set; }
        public bool Status { get; set; }
    }

    /// <summary>
    /// DTO for filtering users
    /// </summary>
    public class UserFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? RoleId { get; set; }
        public bool? Status { get; set; }
        public string? Department { get; set; }
        public string? SearchKeyword { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }

    /// <summary>
    /// DTO representing the result of user Excel import
    /// </summary>
    public class ExcelImportResultDto
    {
        public int Success { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}