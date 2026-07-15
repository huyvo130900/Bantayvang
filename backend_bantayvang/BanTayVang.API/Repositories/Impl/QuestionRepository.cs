using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly BanTayVangDbContext _context;
        
        public QuestionRepository(BanTayVangDbContext context)
        {
            _context = context;
        }

        public async Task<Question?> GetByIdAsync(int id)
        {
            return await _context.Questions
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.QuestionOptions)
                .Include(c => c.ExamPaperQuestions)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .FirstOrDefaultAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<IEnumerable<Question>> GetAllAsync()
        {
            return await _context.Questions
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.QuestionOptions)
                .Where(c => c.DaXoa != true)
                .ToListAsync();
        }

        public async Task<PagedResultDto<Question>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Questions
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.QuestionOptions)
                .Where(c => c.DaXoa != true);

            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<Question>
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

        public async Task<PagedResultDto<Question>> GetFilteredAsync(QuestionFilterDto filter)
        {
            var query = _context.Questions
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.QuestionOptions)
                .Include(c => c.ExamPaperQuestions)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .Where(c => c.DaXoa != true);

            // Apply filters
            if (filter.QuestionCategoryId.HasValue)
                query = query.Where(c => c.QuestionCategoryId == filter.QuestionCategoryId);
                
            if (!string.IsNullOrEmpty(filter.Difficulty))
            {
                var doKhoNorm = filter.Difficulty.Trim().ToLower();
                if (doKhoNorm == "khó" || doKhoNorm == "kho" || doKhoNorm == "3") doKhoNorm = "3";
                else if (doKhoNorm == "trung bình" || doKhoNorm == "trung binh" || doKhoNorm == "trungbinh" || doKhoNorm == "2") doKhoNorm = "2";
                else doKhoNorm = "1";
                query = query.Where(c => c.Difficulty == doKhoNorm);
            }
                
            if (!string.IsNullOrEmpty(filter.Department))
            {
                query = query.Where(c => c.Department == filter.Department);
            }
            else
            {
                query = query.Where(c => c.Department != "Không thuộc ngân hàng");
            }
                
            if (!string.IsNullOrEmpty(filter.SearchKeyword))
                query = query.Where(c => c.Content!.Contains(filter.SearchKeyword));

            if (filter.KyThiId.HasValue)
            {
                query = query.Where(c => c.ExamPaperQuestions.Any(dc => dc.IdDeThiNavigation != null && dc.IdDeThiNavigation.KyThiId == filter.KyThiId.Value));
            }

            if (filter.DeThiId.HasValue)
            {
                query = query.Where(c => c.ExamPaperQuestions.Any(dc => dc.ExamPaperId == filter.DeThiId.Value));
            }

            if (filter.ShowDuplicatesOnly == true)
            {
                query = query.Where(c => !string.IsNullOrEmpty(c.Content) && _context.Questions.Any(other => 
                    other.Id != c.Id && 
                    other.DaXoa != true && 
                    other.Department == c.Department && 
                    !string.IsNullOrEmpty(other.Content) && 
                    other.Content.Trim().ToLower() == c.Content.Trim().ToLower()
                ));
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<Question>
            {
                Items = items,
                Pagination = new PaginationDto
                {
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize)
                }
            };
        }

        public async Task<int> GetFilteredCountAsync(QuestionFilterDto filter)
        {
            var query = _context.Questions.Where(c => c.DaXoa != true);

            if (filter.QuestionCategoryId.HasValue)
                query = query.Where(c => c.QuestionCategoryId == filter.QuestionCategoryId);

            if (!string.IsNullOrEmpty(filter.Difficulty))
            {
                var doKhoNorm = filter.Difficulty.Trim().ToLower();
                if (doKhoNorm == "khó" || doKhoNorm == "kho" || doKhoNorm == "3") doKhoNorm = "3";
                else if (doKhoNorm == "trung bình" || doKhoNorm == "trung binh" || doKhoNorm == "trungbinh" || doKhoNorm == "2") doKhoNorm = "2";
                else doKhoNorm = "1";
                query = query.Where(c => c.Difficulty == doKhoNorm);
            }

            if (!string.IsNullOrEmpty(filter.Department))
            {
                query = query.Where(c => c.Department == filter.Department);
            }
            else
            {
                query = query.Where(c => c.Department != "Không thuộc ngân hàng");
            }

            if (!string.IsNullOrEmpty(filter.SearchKeyword))
                query = query.Where(c => c.Content!.Contains(filter.SearchKeyword));

            if (filter.KyThiId.HasValue)
            {
                query = query.Where(c => c.ExamPaperQuestions.Any(dc => dc.IdDeThiNavigation != null && dc.IdDeThiNavigation.KyThiId == filter.KyThiId.Value));
            }

            if (filter.DeThiId.HasValue)
            {
                query = query.Where(c => c.ExamPaperQuestions.Any(dc => dc.ExamPaperId == filter.DeThiId.Value));
            }

            if (filter.ShowDuplicatesOnly == true)
            {
                query = query.Where(c => !string.IsNullOrEmpty(c.Content) && _context.Questions.Any(other => 
                    other.Id != c.Id && 
                    other.DaXoa != true && 
                    other.Department == c.Department && 
                    !string.IsNullOrEmpty(other.Content) && 
                    other.Content.Trim().ToLower() == c.Content.Trim().ToLower()
                ));
            }

            return await query.CountAsync();
        }

        public async Task<Question?> FindDuplicateAsync(string noiDungChuan, string? department)
        {
            var khoa = string.IsNullOrEmpty(department) ? null : department.Trim();
            return await _context.Questions
                .FirstOrDefaultAsync(c => c.DaXoa != true
                    && c.Content != null
                    && c.Content.ToLower().Trim() == noiDungChuan
                    && (khoa == null ? (c.Department == null || c.Department == "") : c.Department == khoa));
        }

        public async Task<Question?> GetWithChoicesAsync(int id)
        {
            return await _context.Questions
                .Include(c => c.QuestionOptions)
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.ExamPaperQuestions)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .FirstOrDefaultAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<List<int>> GetValidQuestionIdsAsync(List<int> questionIds)
        {
            return await _context.Questions
                .Where(c => questionIds.Contains(c.Id) && c.DaXoa != true)
                .Select(c => c.Id)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<Question> AddAsync(Question entity)
        {
            entity.CreatedAt = DateTime.Now;
            entity.DaXoa = false;
            _context.Questions.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Question> UpdateAsync(Question entity)
        {
            entity.UpdatedAt = DateTime.Now;
            _context.Questions.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Questions.FindAsync(id);
            if (entity == null) return false;
            
            _context.Questions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id, int updatedBy)
        {
            var entity = await _context.Questions.FindAsync(id);
            if (entity == null) return false;
            
            entity.DaXoa = true;
            entity.UpdatedAt = DateTime.Now;
            entity.UpdatedBy = updatedBy;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Questions.AnyAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<List<Question>> GetByKhoaPhongAsync(string department)
        {
            return await _context.Questions
                .Include(c => c.QuestionOptions)
                .Where(c => c.Department == department && c.DaXoa != true)
                .ToListAsync();
        }

        public async Task<List<Question>> GetRandomQuestionsAsync(int count)
        {
            var query = _context.Questions
                .Include(c => c.QuestionOptions)
                .Where(c => c.DaXoa != true && c.Department != "Không thuộc ngân hàng");
                
            return await query
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToListAsync();
        }
    }
}