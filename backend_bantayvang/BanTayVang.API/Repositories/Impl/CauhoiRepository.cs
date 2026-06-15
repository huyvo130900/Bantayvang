using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class CauhoiRepository : ICauhoiRepository
    {
        private readonly BanTayVangDbContext _context;
        
        public CauhoiRepository(BanTayVangDbContext context)
        {
            _context = context;
        }

        public async Task<Cauhoi?> GetByIdAsync(int id)
        {
            return await _context.Cauhois
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.Luachons)
                .Include(c => c.DethiCauhois)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .FirstOrDefaultAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<IEnumerable<Cauhoi>> GetAllAsync()
        {
            return await _context.Cauhois
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.Luachons)
                .Where(c => c.DaXoa != true)
                .ToListAsync();
        }

        public async Task<PagedResultDto<Cauhoi>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Cauhois
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.Luachons)
                .Where(c => c.DaXoa != true);

            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<Cauhoi>
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

        public async Task<PagedResultDto<Cauhoi>> GetFilteredAsync(QuestionFilterDto filter)
        {
            var query = _context.Cauhois
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.Luachons)
                .Include(c => c.DethiCauhois)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .Where(c => c.DaXoa != true);

            // Apply filters
            if (filter.IdLoaiCauHoi.HasValue)
                query = query.Where(c => c.IdLoaiCauHoi == filter.IdLoaiCauHoi);
                
            if (!string.IsNullOrEmpty(filter.DoKho))
            {
                var doKhoNorm = filter.DoKho.Trim().ToLower();
                if (doKhoNorm == "khó" || doKhoNorm == "kho" || doKhoNorm == "3") doKhoNorm = "3";
                else if (doKhoNorm == "trung bình" || doKhoNorm == "trung binh" || doKhoNorm == "trungbinh" || doKhoNorm == "2") doKhoNorm = "2";
                else doKhoNorm = "1";
                query = query.Where(c => c.DoKho == doKhoNorm);
            }
                
            if (!string.IsNullOrEmpty(filter.KhoaPhong))
            {
                query = query.Where(c => c.KhoaPhong == filter.KhoaPhong);
            }
            else
            {
                query = query.Where(c => c.KhoaPhong != "Không thuộc ngân hàng");
            }
                
            if (!string.IsNullOrEmpty(filter.SearchKeyword))
                query = query.Where(c => c.NoiDung!.Contains(filter.SearchKeyword));

            if (filter.KyThiId.HasValue)
            {
                query = query.Where(c => c.DethiCauhois.Any(dc => dc.IdDeThiNavigation != null && dc.IdDeThiNavigation.KyThiId == filter.KyThiId.Value));
            }

            if (filter.DeThiId.HasValue)
            {
                query = query.Where(c => c.DethiCauhois.Any(dc => dc.IdDeThi == filter.DeThiId.Value));
            }

            if (filter.ShowDuplicatesOnly == true)
            {
                query = query.Where(c => !string.IsNullOrEmpty(c.NoiDung) && _context.Cauhois.Any(other => 
                    other.Id != c.Id && 
                    other.DaXoa != true && 
                    other.KhoaPhong == c.KhoaPhong && 
                    !string.IsNullOrEmpty(other.NoiDung) && 
                    other.NoiDung.Trim().ToLower() == c.NoiDung.Trim().ToLower()
                ));
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<Cauhoi>
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
            var query = _context.Cauhois.Where(c => c.DaXoa != true);

            if (filter.IdLoaiCauHoi.HasValue)
                query = query.Where(c => c.IdLoaiCauHoi == filter.IdLoaiCauHoi);

            if (!string.IsNullOrEmpty(filter.DoKho))
            {
                var doKhoNorm = filter.DoKho.Trim().ToLower();
                if (doKhoNorm == "khó" || doKhoNorm == "kho" || doKhoNorm == "3") doKhoNorm = "3";
                else if (doKhoNorm == "trung bình" || doKhoNorm == "trung binh" || doKhoNorm == "trungbinh" || doKhoNorm == "2") doKhoNorm = "2";
                else doKhoNorm = "1";
                query = query.Where(c => c.DoKho == doKhoNorm);
            }

            if (!string.IsNullOrEmpty(filter.KhoaPhong))
            {
                query = query.Where(c => c.KhoaPhong == filter.KhoaPhong);
            }
            else
            {
                query = query.Where(c => c.KhoaPhong != "Không thuộc ngân hàng");
            }

            if (!string.IsNullOrEmpty(filter.SearchKeyword))
                query = query.Where(c => c.NoiDung!.Contains(filter.SearchKeyword));

            if (filter.KyThiId.HasValue)
            {
                query = query.Where(c => c.DethiCauhois.Any(dc => dc.IdDeThiNavigation != null && dc.IdDeThiNavigation.KyThiId == filter.KyThiId.Value));
            }

            if (filter.DeThiId.HasValue)
            {
                query = query.Where(c => c.DethiCauhois.Any(dc => dc.IdDeThi == filter.DeThiId.Value));
            }

            if (filter.ShowDuplicatesOnly == true)
            {
                query = query.Where(c => !string.IsNullOrEmpty(c.NoiDung) && _context.Cauhois.Any(other => 
                    other.Id != c.Id && 
                    other.DaXoa != true && 
                    other.KhoaPhong == c.KhoaPhong && 
                    !string.IsNullOrEmpty(other.NoiDung) && 
                    other.NoiDung.Trim().ToLower() == c.NoiDung.Trim().ToLower()
                ));
            }

            return await query.CountAsync();
        }

        public async Task<Cauhoi?> FindDuplicateAsync(string noiDungChuan, string? khoaPhong)
        {
            var khoa = string.IsNullOrEmpty(khoaPhong) ? null : khoaPhong.Trim();
            return await _context.Cauhois
                .FirstOrDefaultAsync(c => c.DaXoa != true
                    && c.NoiDung != null
                    && c.NoiDung.ToLower().Trim() == noiDungChuan
                    && (khoa == null ? (c.KhoaPhong == null || c.KhoaPhong == "") : c.KhoaPhong == khoa));
        }

        public async Task<Cauhoi?> GetWithChoicesAsync(int id)
        {
            return await _context.Cauhois
                .Include(c => c.Luachons)
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.DethiCauhois)
                    .ThenInclude(dc => dc.IdDeThiNavigation)
                        .ThenInclude(d => d.KyThiNavigation)
                .FirstOrDefaultAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<List<int>> GetValidQuestionIdsAsync(List<int> questionIds)
        {
            return await _context.Cauhois
                .Where(c => questionIds.Contains(c.Id) && c.DaXoa != true)
                .Select(c => c.Id)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<Cauhoi> AddAsync(Cauhoi entity)
        {
            entity.NgayTao = DateTime.Now;
            entity.DaXoa = false;
            _context.Cauhois.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Cauhoi> UpdateAsync(Cauhoi entity)
        {
            entity.NgayCapNhat = DateTime.Now;
            _context.Cauhois.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Cauhois.FindAsync(id);
            if (entity == null) return false;
            
            _context.Cauhois.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id, int nguoiCapNhat)
        {
            var entity = await _context.Cauhois.FindAsync(id);
            if (entity == null) return false;
            
            entity.DaXoa = true;
            entity.NgayCapNhat = DateTime.Now;
            entity.NguoiCapNhat = nguoiCapNhat;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Cauhois.AnyAsync(c => c.Id == id && c.DaXoa != true);
        }

        public async Task<List<Cauhoi>> GetByKhoaPhongAsync(string khoaPhong)
        {
            return await _context.Cauhois
                .Include(c => c.Luachons)
                .Where(c => c.KhoaPhong == khoaPhong && c.DaXoa != true)
                .ToListAsync();
        }

        public async Task<List<Cauhoi>> GetRandomQuestionsAsync(int count)
        {
            var query = _context.Cauhois
                .Include(c => c.Luachons)
                .Where(c => c.DaXoa != true && c.KhoaPhong != "Không thuộc ngân hàng");
                
            return await query
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToListAsync();
        }
    }
}