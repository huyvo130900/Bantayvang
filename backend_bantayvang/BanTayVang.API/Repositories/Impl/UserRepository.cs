using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    /// <summary>
    /// User account repository implementation
    /// OWASP A01: Broken Access Control prevention
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            if (string.IsNullOrEmpty(usernameOrEmail))
                return null;

            // BUG FIX: Include ManagedDepartment để AuthService có thể lấy DepartmentName gán vào UserInfoDto.
            // Nếu thiếu Include này thì ManagedDepartment = null → DeptManagerDeptName không bao giờ được set
            // → Frontend filter theo department bị null → GET /api/Question không kèm department param
            // → Backend DeptManager scope trả về danh sách rỗng vì filter.Department = null.
            return await _dbSet
                .Include(u => u.ManagedDepartment)
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.EmployeeCode == usernameOrEmail);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            return await _dbSet
                .Include(u => u.ManagedDepartment)
                .FirstOrDefaultAsync(u => u.Username == username || u.EmployeeCode == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            var query = _dbSet.Where(u => u.Username == username);
            
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

        public async Task<List<User>> GetByRoleAsync(int roleId)
        {
            return await _dbSet
                .Where(u => u.RoleId == roleId && u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(u => u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<bool> UpdateLastLoginAsync(int userId, DateTime loginTime)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.LastLoginAt = loginTime;
                await UpdateAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Override to include ManagedDepartment navigation for DeptManager display</summary>
        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet
                .Include(u => u.ManagedDepartment)
                .ToListAsync();
        }

        /// <summary>Override GetByIdAsync to include ManagedDepartment so DeptManagerDeptName is available in AuthService</summary>
        public override async Task<User?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(u => u.ManagedDepartment)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}