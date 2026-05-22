using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class LuachonRepository : ILuachonRepository
    {
        private readonly BanTayVangDbContext _context;
        
        public LuachonRepository(BanTayVangDbContext context)
        {
            _context = context;
        }

        public async Task<Luachon?> GetByIdAsync(int id)
        {
            return await _context.Luachons.FindAsync(id);
        }

        public async Task<IEnumerable<Luachon>> GetAllAsync()
        {
            return await _context.Luachons.ToListAsync();
        }

        public async Task<PagedResultDto<Luachon>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Luachons.AsQueryable();
            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<Luachon>
            {
                Items = items,
                Pagination = new PaginationDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                }
            };
        }

        public async Task<Luachon> AddAsync(Luachon entity)
        {
            _context.Luachons.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Luachon> UpdateAsync(Luachon entity)
        {
            _context.Luachons.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Luachons.FindAsync(id);
            if (entity == null) return false;
            
            _context.Luachons.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Luachons.AnyAsync(l => l.Id == id);
        }

        public async Task<List<Luachon>> GetByCauhoiIdAsync(int cauhoiId)
        {
            return await _context.Luachons
                .Where(l => l.IdCauHoi == cauhoiId)
                .OrderBy(l => l.ThuTu)
                .ToListAsync();
        }

        public async Task<bool> DeleteByCauhoiIdAsync(int cauhoiId)
        {
            var luachons = await _context.Luachons
                .Where(l => l.IdCauHoi == cauhoiId)
                .ToListAsync();
                
            _context.Luachons.RemoveRange(luachons);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByQuestionIdAsync(int questionId)
        {
            return await DeleteByCauhoiIdAsync(questionId);
        }
    }
}