using System.Security.Claims;

namespace BanTayVang.API.Helpers
{
    /// <summary>
    /// Extracts department context from JWT for DeptManager role-based filtering.
    /// </summary>
    public static class DepartmentAuthHelper
    {
        public static bool IsAdmin(ClaimsPrincipal user)
            => user.IsInRole("Admin");

        public static bool IsDeptManager(ClaimsPrincipal user)
            => user.IsInRole("DeptManager");

        /// <summary>
        /// Returns null for Admin (unrestricted), or the DeptManagerDeptId for DeptManager.
        /// </summary>
        public static int? GetDeptManagerKhoaId(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return null;
            var claim = user.FindFirst("id_khoa_quan_ly")?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        public static string? GetKhoaPhong(ClaimsPrincipal user)
            => user.FindFirst("khoa_phong")?.Value;

        public static int? GetUserId(ClaimsPrincipal user)
        {
            var val = user.FindFirst("user_id")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        public static bool CanAccessKhoa(ClaimsPrincipal user, string? department)
        {
            if (IsAdmin(user)) return true;
            if (!IsDeptManager(user)) return false;
            var myKhoa = GetKhoaPhong(user);
            return string.IsNullOrEmpty(myKhoa) || myKhoa == department;
        }
    }
}
