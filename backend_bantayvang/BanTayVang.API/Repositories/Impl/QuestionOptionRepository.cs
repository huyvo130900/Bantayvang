using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Repositories.Impl
{
    public class QuestionOptionRepository : IQuestionOptionRepository
    {
        private readonly BanTayVangDbContext _context;
        
        public QuestionOptionRepository(BanTayVangDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionOption?> GetByIdAsync(int id)
        {
            return await _context.QuestionOptions.FindAsync(id);
        }

        public async Task<IEnumerable<QuestionOption>> GetAllAsync()
        {
            return await _context.QuestionOptions.ToListAsync();
        }

        public async Task<PagedResultDto<QuestionOption>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.QuestionOptions.AsQueryable();
            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<QuestionOption>
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

        public async Task<QuestionOption> AddAsync(QuestionOption entity)
        {
            _context.QuestionOptions.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<QuestionOption> UpdateAsync(QuestionOption entity)
        {
            _context.QuestionOptions.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.QuestionOptions.FindAsync(id);
            if (entity == null) return false;
            
            _context.QuestionOptions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.QuestionOptions.AnyAsync(l => l.Id == id);
        }

        public async Task<List<QuestionOption>> GetByCauhoiIdAsync(int questionId)
        {
            return await _context.QuestionOptions
                .Where(l => l.QuestionId == questionId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();
        }

        public async Task<bool> DeleteByCauhoiIdAsync(int questionId)
        {
            var questionOptions = await _context.QuestionOptions
                .Where(l => l.QuestionId == questionId)
                .ToListAsync();
                
            _context.QuestionOptions.RemoveRange(questionOptions);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByQuestionIdAsync(int questionId)
        {
            return await DeleteByCauhoiIdAsync(questionId);
        }
    }
}