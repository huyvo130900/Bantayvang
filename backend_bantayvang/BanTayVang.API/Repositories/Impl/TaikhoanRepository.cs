using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    /// <summary>
    /// User account repository implementation
    /// OWASP A01: Broken Access Control prevention
    /// </summary>
    public class TaikhoanRepository : BaseRepository<Taikhoan>, ITaikhoanRepository
    {
        public TaikhoanRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<Taikhoan?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            if (string.IsNullOrEmpty(usernameOrEmail))
                return null;

            // BUG FIX: Include KhoaQuanLy để AuthService có thể lấy TenKhoa gán vào UserInfoDto.
            // Nếu thiếu Include này thì KhoaQuanLy = null → TenKhoaQuanLy không bao giờ được set
            // → Frontend filter theo khoaPhong bị null → GET /api/Cauhoi không kèm khoaPhong param
            // → Backend DeptManager scope trả về danh sách rỗng vì filter.KhoaPhong = null.
            return await _dbSet
                .Include(u => u.KhoaQuanLy)
                .FirstOrDefaultAsync(u => u.TenDangNhap == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<Taikhoan?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            return await _dbSet
                .Include(u => u.KhoaQuanLy)
                .FirstOrDefaultAsync(u => u.TenDangNhap == username);
        }

        public async Task<Taikhoan?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            var query = _dbSet.Where(u => u.TenDangNhap == username);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var query = _dbSet.Where(u => u.Email == email);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Taikhoan>> GetByRoleAsync(int roleId)
        {
            return await _dbSet
                .Where(u => u.IdVaiTro == roleId && u.TrangThai == true)
                .OrderBy(u => u.HoTen)
                .ToListAsync();
        }

        public async Task<List<Taikhoan>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(u => u.TrangThai == true)
                .OrderBy(u => u.HoTen)
                .ToListAsync();
        }

        public async Task<bool> UpdateLastLoginAsync(int userId, DateTime loginTime)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.LanDangNhapCuoi = loginTime;
                await UpdateAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Override to include KhoaQuanLy navigation for DeptManager display</summary>
        public override async Task<IEnumerable<Taikhoan>> GetAllAsync()
        {
            return await _dbSet
                .Include(u => u.KhoaQuanLy)
                .ToListAsync();
        }

        /// <summary>Override GetByIdAsync to include KhoaQuanLy so TenKhoaQuanLy is available in AuthService</summary>
        public override async Task<Taikhoan?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(u => u.KhoaQuanLy)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}