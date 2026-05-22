using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class GradingService : Services.Interfaces.IGradingService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<GradingService> _logger;
        private const double PassScore = 5.0;

        public GradingService(BanTayVangDbContext context, ILogger<GradingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<ExamResultDetailDto>> GetResultDetailAsync(int baiThiId)
        {
            try
            {
                var baithi = await _context.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.Luachons)
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdLuaChonDaChonNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);

                if (baithi == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                var detail = MapToDetailDto(baithi);

                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting result detail");
                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByExamAsync(int examId)
        {
            try
            {
                var baithis = await _context.Baithis
                    .Where(b => b.IdDeThi == examId)
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .OrderByDescending(b => b.TongDiem)
                    .ToListAsync();

                var results = baithis.Select(b => MapToDetailDtoSummary(b)).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results by exam");
                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamResultDetailDto>()
                };
            }
        }

        public async Task<BaseResponseDto<ExamResultDetailDto>> RegradeAsync(int baiThiId)
        {
            try
            {
                var baithi = await _context.Baithis
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.Luachons)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);

                if (baithi == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                int correctCount = 0;
                double totalScore = 0;

                // Group answers by question to support multiple-choice
                var answersByQuestion = baithi.Chitietlambais
                    .Where(c => c.IdCauHoi.HasValue)
                    .GroupBy(c => c.IdCauHoi!.Value);

                foreach (var group in answersByQuestion)
                {
                    var question = group.First().IdCauHoiNavigation;
                    if (question == null) continue;

                    var correctChoiceIds = question.Luachons
                        .Where(l => l.LaDapAnDung == true)
                        .Select(l => l.Id)
                        .ToHashSet();

                    var userChoiceIds = group
                        .Where(c => c.IdLuaChonDaChon.HasValue)
                        .Select(c => c.IdLuaChonDaChon!.Value)
                        .ToHashSet();

                    bool isFullyCorrect = correctChoiceIds.Count > 0 
                        && correctChoiceIds.SetEquals(userChoiceIds);

                    foreach (var ct in group)
                    {
                        if (isFullyCorrect)
                        {
                            ct.DiemDatDuoc = (question.Diem ?? 1) / Math.Max(1, group.Count());
                        }
                        else
                        {
                            ct.DiemDatDuoc = 0;
                        }
                    }

                    if (isFullyCorrect)
                    {
                        correctCount++;
                        totalScore += question.Diem ?? 1;
                    }
                }

                baithi.SoCauDung = correctCount;
                baithi.TongDiem = totalScore;

                await _context.SaveChangesAsync();

                return await GetResultDetailAsync(baiThiId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regrading");
                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto> ManualGradeAsync(ManualGradingDto dto)
        {
            try
            {
                var chitiet = await _context.Chitietlambais
                    .Include(c => c.IdBaiThiNavigation)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChiTietLamBaiId);

                if (chitiet == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy chi tiết bài làm" };

                chitiet.DiemDatDuoc = dto.DiemDatDuoc;
                await _context.SaveChangesAsync();

                // Recalculate total score
                if (chitiet.IdBaiThi.HasValue)
                {
                    var baithi = await _context.Baithis
                        .Include(b => b.Chitietlambais)
                        .FirstOrDefaultAsync(b => b.Id == chitiet.IdBaiThi.Value);
                    
                    if (baithi != null)
                    {
                        baithi.TongDiem = baithi.Chitietlambais.Sum(c => c.DiemDatDuoc ?? 0);
                        await _context.SaveChangesAsync();
                    }
                }

                return new BaseResponseDto { Success = true, Message = "Chấm điểm thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manual grading");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetRankingByExamAsync(int examId, int top = 50)
        {
            try
            {
                var baithis = await _context.Baithis
                    .Where(b => b.IdDeThi == examId && b.TrangThai == "Completed")
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .OrderByDescending(b => b.TongDiem)
                    .ThenBy(b => b.ThoiGianNop) // tie-break by submission time
                    .Take(top)
                    .ToListAsync();

                var results = baithis.Select(b => MapToDetailDtoSummary(b)).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ranking");
                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamResultDetailDto>()
                };
            }
        }

        public async Task<BaseResponseDto<int>> AutoGradeAllAsync()
        {
            try
            {
                var ungraded = await _context.Baithis
                    .Where(b => b.TrangThai == "Completed" && (b.TongDiem == null || b.SoCauDung == null))
                    .ToListAsync();

                int count = 0;
                foreach (var b in ungraded)
                {
                    await RegradeAsync(b.Id);
                    count++;
                }

                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = $"Đã chấm lại {count} bài thi",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-grading all");
                return new BaseResponseDto<int>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = 0
                };
            }
        }

        #region Private Helpers

        private ExamResultDetailDto MapToDetailDto(Baithi baithi)
        {
            int? duration = null;
            if (baithi.ThoiGianBatDau.HasValue && baithi.ThoiGianNop.HasValue)
            {
                duration = (int)(baithi.ThoiGianNop.Value - baithi.ThoiGianBatDau.Value).TotalMinutes;
            }

            var detail = new ExamResultDetailDto
            {
                BaiThiId = baithi.Id,
                UserId = baithi.IdTaiKhoan,
                Username = baithi.IdTaiKhoanNavigation?.TenDangNhap,
                FullName = baithi.IdTaiKhoanNavigation?.HoTen,
                KhoaPhong = baithi.IdTaiKhoanNavigation?.KhoaPhong,
                ExamId = baithi.IdDeThi ?? 0,
                MaDeThi = baithi.IdDeThiNavigation?.MaDeThi,
                TenDeThi = baithi.IdDeThiNavigation?.TenDeThi,
                ThoiGianBatDau = baithi.ThoiGianBatDau,
                ThoiGianNop = baithi.ThoiGianNop,
                DurationMinutes = duration,
                TongDiem = baithi.TongDiem,
                SoCauDung = baithi.SoCauDung,
                TongSoCau = baithi.TongSoCau,
                TrangThai = baithi.TrangThai,
                Pass = (baithi.TongDiem ?? 0) >= PassScore,
                SoCanhBao = baithi.TongSoCanhBao,
                Answers = new List<AnswerDetailDto>()
            };

            foreach (var ct in baithi.Chitietlambais)
            {
                var question = ct.IdCauHoiNavigation;
                if (question == null) continue;

                var correctChoice = question.Luachons.FirstOrDefault(l => l.LaDapAnDung == true);

                detail.Answers.Add(new AnswerDetailDto
                {
                    CauHoiId = question.Id,
                    NoiDungCauHoi = question.NoiDung,
                    Diem = question.Diem,
                    IdLuaChonDaChon = ct.IdLuaChonDaChon,
                    NoiDungDapAn = ct.IdLuaChonDaChonNavigation?.NoiDung,
                    CauTraLoiTuLuan = ct.CauTraLoiTuLuan,
                    IsCorrect = correctChoice != null && ct.IdLuaChonDaChon == correctChoice.Id,
                    DiemDatDuoc = ct.DiemDatDuoc,
                    IdLuaChonDung = correctChoice?.Id,
                    NoiDungDapAnDung = correctChoice?.NoiDung
                });
            }

            return detail;
        }

        private ExamResultDetailDto MapToDetailDtoSummary(Baithi baithi)
        {
            int? duration = null;
            if (baithi.ThoiGianBatDau.HasValue && baithi.ThoiGianNop.HasValue)
            {
                duration = (int)(baithi.ThoiGianNop.Value - baithi.ThoiGianBatDau.Value).TotalMinutes;
            }

            return new ExamResultDetailDto
            {
                BaiThiId = baithi.Id,
                UserId = baithi.IdTaiKhoan,
                Username = baithi.IdTaiKhoanNavigation?.TenDangNhap,
                FullName = baithi.IdTaiKhoanNavigation?.HoTen,
                KhoaPhong = baithi.IdTaiKhoanNavigation?.KhoaPhong,
                ExamId = baithi.IdDeThi ?? 0,
                MaDeThi = baithi.IdDeThiNavigation?.MaDeThi,
                TenDeThi = baithi.IdDeThiNavigation?.TenDeThi,
                ThoiGianBatDau = baithi.ThoiGianBatDau,
                ThoiGianNop = baithi.ThoiGianNop,
                DurationMinutes = duration,
                TongDiem = baithi.TongDiem,
                SoCauDung = baithi.SoCauDung,
                TongSoCau = baithi.TongSoCau,
                TrangThai = baithi.TrangThai,
                Pass = (baithi.TongDiem ?? 0) >= PassScore,
                SoCanhBao = baithi.TongSoCanhBao
            };
        }

        #endregion
    }
}