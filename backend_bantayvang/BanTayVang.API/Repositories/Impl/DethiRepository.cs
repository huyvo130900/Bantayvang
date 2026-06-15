using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BanTayVang.API.Repositories.Impl
{
    public class DethiRepository : BaseRepository<Dethi>, IDethiRepository
    {
        public DethiRepository(BanTayVangDbContext context) : base(context)
        {
        }

        public async Task<Dethi?> GetByMaDeThiAsync(string maDeThi)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi);
        }

        public async Task<Dethi?> GetByMaDeThiAsync(string maDeThi, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi, cancellationToken);
        }

        public async Task<List<Dethi>> GetActiveExamsAsync()
        {
            return await _dbSet
                .Where(d => d.TrangThai == "Active" || d.TrangThai == "Published")
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<List<Dethi>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.TrangThai == "Active" || d.TrangThai == "Published")
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dethi?> GetWithQuestionsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.DethiCauhois)
                    .ThenInclude(dc => dc.IdCauHoiNavigation)
                        .ThenInclude(c => c!.Luachons)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Dethi?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.DethiCauhois)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<bool> AddQuestionsToExamAsync(int dethiId, List<int> cauhoiIds)
        {
            try
            {
                // Xóa câu hỏi cũ
                var existingQuestions = await _context.DethiCauhois
                    .Where(dc => dc.IdDeThi == dethiId)
                    .ToListAsync();
                _context.DethiCauhois.RemoveRange(existingQuestions);

                // Thêm câu hỏi mới
                foreach (var cauhoiId in cauhoiIds)
                {
                    _context.DethiCauhois.Add(new DethiCauhoi
                    {
                        IdDeThi = dethiId,
                        IdCauHoi = cauhoiId
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateExamQuestionsAsync(int examId, List<int> questionIds)
        {
            try
            {
                // Remove existing questions
                var existingQuestions = await _context.DethiCauhois
                    .Where(dc => dc.IdDeThi == examId)
                    .ToListAsync();
                _context.DethiCauhois.RemoveRange(existingQuestions);

                // Add new questions
                foreach (var questionId in questionIds)
                {
                    _context.DethiCauhois.Add(new DethiCauhoi
                    {
                        IdDeThi = examId,
                        IdCauHoi = questionId
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Dethi>> GetAllExamsAsync(string? trangThai = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(d => d.DethiCauhois)
                .AsQueryable();
            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(d => d.TrangThai == trangThai);
            return await query
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Dethi>> GetExamsByKyThiAsync(int kyThiId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.KyThiId == kyThiId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dethi?> ResolveExamForCandidateAsync(int kyThiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            var kyThiExams = await _dbSet
                .Where(d => d.KyThiId == kyThiId)
                .OrderBy(d => d.Id)
                .ToListAsync(cancellationToken);

            if (!kyThiExams.Any())
            {
                return null;
            }

            // 1. Check if the candidate has an active (InProgress/Paused) session in this KyThi.
            // If they do, they must resume it, so return the exam associated with that session.
            var activeSession = await _context.Baithis
                .Where(b => b.IdTaiKhoan == taikhoanId 
                         && b.IdKyThi == kyThiId 
                         && b.IdDeThi.HasValue
                         && (b.TrangThai == "InProgress" || b.TrangThai == "Paused"))
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSession != null)
            {
                var exam = kyThiExams.FirstOrDefault(d => d.Id == activeSession.IdDeThi!.Value);
                if (exam != null)
                {
                    return exam;
                }
            }

            // 2. Candidate is starting a new attempt.
            // Retrieve all past sessions of this candidate in this KyThi.
            var pastSessions = await _context.Baithis
                .Where(b => b.IdTaiKhoan == taikhoanId 
                         && b.IdKyThi == kyThiId 
                         && b.IdDeThi.HasValue)
                .ToListAsync(cancellationToken);

            // Group and count attempts per exam ID
            var examAttemptCounts = pastSessions
                .GroupBy(b => b.IdDeThi!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // 3. Determine the preferred starting exam index for the candidate
            int startIndex = -1;
            var kyThi = await _context.Set<KyThi>().FindAsync(new object[] { kyThiId }, cancellationToken);
            if (kyThi != null && kyThi.KhoaPhongId.HasValue)
            {
                var khoa = await _context.Set<KhoaPhong>().FindAsync(new object[] { kyThi.KhoaPhongId.Value }, cancellationToken);
                if (khoa != null)
                {
                    var students = await _context.Taikhoans
                        .Where(u => u.KhoaPhong == khoa.TenKhoa && u.IdVaiTro == 3)
                        .OrderBy(u => u.Id)
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);

                    var studentIndex = students.IndexOf(taikhoanId);
                    if (studentIndex >= 0)
                    {
                        startIndex = studentIndex % kyThiExams.Count;
                    }
                }
            }

            if (startIndex == -1)
            {
                startIndex = taikhoanId % kyThiExams.Count;
            }

            // 4. Sort the exams by the number of attempts (ascending), and then by the preferred sequence order (ascending)
            int N = kyThiExams.Count;
            var sortedExams = kyThiExams
                .Select((exam, i) => new
                {
                    Exam = exam,
                    Attempts = examAttemptCounts.TryGetValue(exam.Id, out var count) ? count : 0,
                    OrderIndex = (i - startIndex + N) % N
                })
                .OrderBy(x => x.Attempts)
                .ThenBy(x => x.OrderIndex)
                .Select(x => x.Exam)
                .ToList();

            return sortedExams.FirstOrDefault();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet
                .Include(d => d.DethiCauhois)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null)
                return false;

            // Delete associated DethiCauhois first to prevent FK violation
            if (entity.DethiCauhois != null && entity.DethiCauhois.Any())
            {
                _context.DethiCauhois.RemoveRange(entity.DethiCauhois);
            }

            // Delete associated ExamAssignments if any
            var assignments = await _context.ExamAssignments
                .Where(a => a.ExamId == id)
                .ToListAsync();
            if (assignments.Any())
            {
                _context.ExamAssignments.RemoveRange(assignments);
            }

            // Delete associated Baithis and their children (Canhbaogianlans and Chitietlambais) if any
            var baithis = await _context.Baithis
                .Include(b => b.Canhbaogianlans)
                .Include(b => b.Chitietlambais)
                .Where(b => b.IdDeThi == id)
                .ToListAsync();
            if (baithis.Any())
            {
                foreach (var bt in baithis)
                {
                    if (bt.Canhbaogianlans != null && bt.Canhbaogianlans.Any())
                        _context.Canhbaogianlans.RemoveRange(bt.Canhbaogianlans);
                    if (bt.Chitietlambais != null && bt.Chitietlambais.Any())
                        _context.Chitietlambais.RemoveRange(bt.Chitietlambais);
                }
                _context.Baithis.RemoveRange(baithis);
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}