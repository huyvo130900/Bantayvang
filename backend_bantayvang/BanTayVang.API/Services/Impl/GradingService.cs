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
                    .Include(b => b.KyThiNavigation)
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.Luachons)
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.IdLoaiCauHoiNavigation)
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdLuaChonDaChonNavigation)
                    .FirstOrDefaultAsync(b => b.Id == baiThiId);

                if (baithi == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                var detail = MapToDetailDto(baithi);

                if (baithi.IdTaiKhoan != null && baithi.IdDeThi != null)
                {
                    var attempts = await _context.Baithis
                        .Where(b => b.IdTaiKhoan == baithi.IdTaiKhoan && b.IdDeThi == baithi.IdDeThi && (b.TrangThai == "Completed" || b.Id == baithi.Id))
                        .ToListAsync();

                    detail.SoLanThi = attempts.Count;
                    detail.SoLanGianLan = attempts.Sum(b => b.TongSoCanhBao ?? 0);
                    detail.SoLanThiLai = Math.Max(0, attempts.Count - 1);
                }

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
                // Lấy TẤT CẢ bài thi của đề này (kể cả thi lại) để tính số lần thi
                var allBaithis = await _context.Baithis
                    .Where(b => b.IdDeThi == examId && b.TrangThai == "Completed")
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .OrderByDescending(b => b.ThoiGianNop)
                    .ToListAsync();

                // Group theo user, lấy bài thi gần nhất (điểm mới nhất)
                var latestPerUser = allBaithis
                    .GroupBy(b => b.IdTaiKhoan)
                    .Select(g => g.First())
                    .ToList();

                // Đếm số lần thi và tổng gian lận mỗi user
                var countPerUser = allBaithis
                    .GroupBy(b => b.IdTaiKhoan)
                    .ToDictionary(
                        g => g.Key ?? 0,
                        g => new {
                            SoLanThi = g.Count(),
                            SoLanGianLan = g.Sum(b => b.TongSoCanhBao ?? 0)
                        });

                var latestPerUserIds = latestPerUser.Select(b => b.Id).ToList();
                var gradedCounts = await _context.Chitietlambais
                    .Where(c => c.IdBaiThi != null && latestPerUserIds.Contains(c.IdBaiThi.Value) && c.DiemDatDuoc != null)
                    .GroupBy(c => c.IdBaiThi!.Value)
                    .Select(g => new { BaiThiId = g.Key, GradedCount = g.Count() })
                    .ToDictionaryAsync(x => x.BaiThiId, x => x.GradedCount);

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (latestPerUserIds.Any())
                {
                    var chitietStats = await _context.Chitietlambais
                        .Where(c => c.IdBaiThi != null && latestPerUserIds.Contains(c.IdBaiThi.Value))
                        .Select(c => new {
                            c.IdBaiThi,
                            IsGraded = c.DiemDatDuoc != null,
                            IsEssay = c.IdCauHoiNavigation != null 
                                && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null 
                                && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" 
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan")
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.IdBaiThi!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                mcqTotal: g.Count(c => !c.IsEssay),
                                mcqGraded: g.Count(c => !c.IsEssay && c.IsGraded),
                                essayTotal: g.Count(c => c.IsEssay),
                                essayGraded: g.Count(c => c.IsEssay && c.IsGraded)
                            ));
                }

                var results = latestPerUser.Select(b =>
                {
                    var dto = MapToDetailDtoSummary(b);
                    if (countPerUser.TryGetValue(b.IdTaiKhoan ?? 0, out var counts))
                    {
                        dto.SoLanThi = counts.SoLanThi;
                        dto.SoLanGianLan = counts.SoLanGianLan;
                        dto.SoLanThiLai = counts.SoLanThi - 1;
                    }
                    dto.SoCauDaCham = gradedCounts.TryGetValue(b.Id, out var gc) ? gc : 0;
                    if (statsDict.TryGetValue(b.Id, out var stats))
                    {
                        dto.TongSoCauTracNghiem = stats.mcqTotal;
                        dto.SoCauTracNghiemDaCham = stats.mcqGraded;
                        dto.TongSoCauTuLuan = stats.essayTotal;
                        dto.SoCauTuLuanDaCham = stats.essayGraded;
                    }
                    return dto;
                }).ToList();

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
                    .Include(b => b.Chitietlambais)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.IdLoaiCauHoiNavigation)
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

                    var tenLoai = question.IdLoaiCauHoiNavigation?.TenLoai;
                    var isEssay = tenLoai == "Tự luận" || tenLoai == "TuLuan" || correctChoiceIds.Count == 0;

                    if (isEssay)
                    {
                        // For essay, preserve existing DiemDatDuoc which is manually graded
                        var score = group.FirstOrDefault()?.DiemDatDuoc ?? 0;
                        if (score >= 1)
                        {
                            correctCount++;
                            totalScore += 1;
                        }
                    }
                    else
                    {
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
                                ct.DiemDatDuoc = 1.0 / Math.Max(1, group.Count());
                            }
                            else
                            {
                                ct.DiemDatDuoc = 0;
                            }
                        }

                        if (isFullyCorrect)
                        {
                            correctCount++;
                            totalScore += 1;
                        }
                    }
                }

                baithi.SoCauDung = correctCount;
                var tongSoCau = baithi.TongSoCau ?? answersByQuestion.Count();
                baithi.TongDiem = correctCount;

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
                    .Include(c => c.IdCauHoiNavigation)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChiTietLamBaiId);

                if (chitiet == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy chi tiết bài làm" };

                // Đánh dấu Đúng/Sai: DiemDatDuoc = 1 nếu đúng, = 0 nếu sai
                chitiet.DiemDatDuoc = dto.IsCorrect ? 1.0 : 0.0;
                if (!string.IsNullOrEmpty(dto.NhanXet))
                    chitiet.CauTraLoiTuLuan = chitiet.CauTraLoiTuLuan; // giữ nguyên nội dung

                await _context.SaveChangesAsync();

                // Tính lại tổng số câu đúng cho bài thi
                if (chitiet.IdBaiThi.HasValue)
                {
                    var baithi = await _context.Baithis
                        .Include(b => b.Chitietlambais)
                            .ThenInclude(c => c.IdCauHoiNavigation)
                        .FirstOrDefaultAsync(b => b.Id == chitiet.IdBaiThi.Value);
                    
                    if (baithi != null)
                    {
                        // Tính số câu đúng: trắc nghiệm (isCorrect = là câu chọn đúng) + tự luận (DiemDatDuoc == 1)
                        int soCauDung = 0;
                        foreach (var ct in baithi.Chitietlambais)
                        {
                            var tenLoai = ct.IdCauHoiNavigation?.IdLoaiCauHoiNavigation?.TenLoai;
                            bool isTuLuan = tenLoai == "Tự luận" || tenLoai == "TuLuan";
                            if (isTuLuan)
                            {
                                if (ct.DiemDatDuoc == 1.0) soCauDung++;
                            }
                            else
                            {
                                // Trắc nghiệm: đã được chấm tự động, DiemDatDuoc > 0 nghĩa là đúng
                                if ((ct.DiemDatDuoc ?? 0) > 0) soCauDung++;
                            }
                        }
                        baithi.SoCauDung = soCauDung;
                        baithi.TongDiem = soCauDung;
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
                    .Include(b => b.KyThiNavigation)
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
                MaNhanVien = baithi.IdTaiKhoanNavigation?.MaNhanVien,
                KhoaPhong = baithi.IdTaiKhoanNavigation?.KhoaPhong,
                ExamId = baithi.IdDeThi ?? 0,
                IdDeThi = baithi.IdDeThi,
                MaDeThi = baithi.IdDeThiNavigation?.MaDeThi,
                TenDeThi = baithi.IdDeThiNavigation?.TenDeThi,
                ThoiGianBatDau = baithi.ThoiGianBatDau,
                ThoiGianNop = baithi.ThoiGianNop,
                DurationMinutes = duration,
                TongDiem = baithi.SoCauDung,
                SoCauDung = baithi.SoCauDung,
                TongSoCau = baithi.TongSoCau,
                TrangThai = baithi.TrangThai,
                Pass = baithi.KyThiNavigation?.SoCauDungToiThieu != null 
                    ? (baithi.SoCauDung ?? 0) >= baithi.KyThiNavigation.SoCauDungToiThieu.Value 
                    : (baithi.IdDeThiNavigation?.SoCauDungToiThieu != null 
                        ? (baithi.SoCauDung ?? 0) >= baithi.IdDeThiNavigation.SoCauDungToiThieu.Value 
                        : true),
                SoCauDungToiThieu = baithi.KyThiNavigation?.SoCauDungToiThieu ?? baithi.IdDeThiNavigation?.SoCauDungToiThieu,
                SoCanhBao = baithi.TongSoCanhBao,
                CongBoKetQua = baithi.CongBoRieng || (baithi.IdDeThiNavigation?.CongBoKetQua ?? false),
                Answers = new List<AnswerDetailDto>()
            };

            int mcqTotal = 0;
            int mcqGraded = 0;
            int essayTotal = 0;
            int essayGraded = 0;

            foreach (var ct in baithi.Chitietlambais)
            {
                var question = ct.IdCauHoiNavigation;
                if (question == null) continue;

                var tenLoai = question.IdLoaiCauHoiNavigation?.TenLoai;
                bool isEssay = tenLoai == "Tự luận" || tenLoai == "TuLuan";

                if (isEssay)
                {
                    essayTotal++;
                    if (ct.DiemDatDuoc != null) essayGraded++;
                }
                else
                {
                    mcqTotal++;
                    if (ct.DiemDatDuoc != null) mcqGraded++;
                }

                var correctChoice = question.Luachons.FirstOrDefault(l => l.LaDapAnDung == true);

                detail.Answers.Add(new AnswerDetailDto
                {
                    CauHoiId = question.Id,
                    NoiDungCauHoi = question.NoiDung,
                    LoaiCauHoi = question.IdLoaiCauHoiNavigation?.TenLoai,
                    IdLuaChonDaChon = ct.IdLuaChonDaChon,
                    NoiDungDapAn = ct.IdLuaChonDaChonNavigation?.NoiDung,
                    CauTraLoiTuLuan = ct.CauTraLoiTuLuan,
                    IsCorrect = correctChoice != null && ct.IdLuaChonDaChon == correctChoice.Id,
                    DiemDatDuoc = ct.DiemDatDuoc,
                    IdLuaChonDung = correctChoice?.Id,
                    NoiDungDapAnDung = correctChoice?.NoiDung,
                    ChiTietLamBaiId = ct.Id
                });
            }

            detail.SoCauDaCham = baithi.Chitietlambais.Count(c => c.DiemDatDuoc != null);
            detail.TongSoCauTracNghiem = mcqTotal;
            detail.SoCauTracNghiemDaCham = mcqGraded;
            detail.TongSoCauTuLuan = essayTotal;
            detail.SoCauTuLuanDaCham = essayGraded;

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
                MaNhanVien = baithi.IdTaiKhoanNavigation?.MaNhanVien,
                KhoaPhong = baithi.IdTaiKhoanNavigation?.KhoaPhong,
                ExamId = baithi.IdDeThi ?? 0,
                MaDeThi = baithi.MaDeThi ?? baithi.IdDeThiNavigation?.MaDeThi,
                TenDeThi = baithi.IdDeThiNavigation?.TenDeThi,
                ThoiGianBatDau = baithi.ThoiGianBatDau,
                ThoiGianNop = baithi.ThoiGianNop,
                DurationMinutes = duration,
                TongDiem = baithi.SoCauDung,
                SoCauDung = baithi.SoCauDung,
                TongSoCau = baithi.TongSoCau,
                TrangThai = baithi.TrangThai,
                Pass = baithi.KyThiNavigation?.SoCauDungToiThieu != null 
                    ? (baithi.SoCauDung ?? 0) >= baithi.KyThiNavigation.SoCauDungToiThieu.Value 
                    : (baithi.IdDeThiNavigation?.SoCauDungToiThieu != null 
                        ? (baithi.SoCauDung ?? 0) >= baithi.IdDeThiNavigation.SoCauDungToiThieu.Value 
                        : true),
                SoCauDungToiThieu = baithi.KyThiNavigation?.SoCauDungToiThieu ?? baithi.IdDeThiNavigation?.SoCauDungToiThieu,
                SoCanhBao = baithi.TongSoCanhBao,
                CongBoKetQua = baithi.CongBoRieng || (baithi.IdDeThiNavigation?.CongBoKetQua ?? false),
                DanhGiaKhoa = baithi.DanhGiaKhoa,
            };
        }


        /// <summary>
        /// Quản lý khoa đánh giá / nhận xét bài thi của thí sinh
        /// </summary>
        public async Task<BaseResponseDto> DanhGiaThiSinhAsync(int baiThiId, string danhGia)
        {
            try
            {
                var baithi = await _context.Baithis.FindAsync(baiThiId);
                if (baithi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };

                baithi.DanhGiaKhoa = danhGia;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã lưu đánh giá" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving DanhGiaKhoa for baithi {BaiThiId}", baiThiId);
                return new BaseResponseDto { Success = false, Message = "Lỗi khi lưu đánh giá" };
            }
        }

        #endregion

        /// <summary>
        /// Lấy kết quả thi theo Kỳ thi
        /// </summary>
        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByKyThiAsync(int kyThiId)
        {
            try
            {
                var baithis = await _context.Baithis
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .Where(b => b.IdKyThi == kyThiId)
                    .OrderByDescending(b => b.TongDiem)
                    .ToListAsync();

                var userIds = baithis.Select(b => b.IdTaiKhoan).Distinct().ToList();
                var examIds = baithis.Select(b => b.IdDeThi).Distinct().ToList();
                var baithiIds = baithis.Select(b => b.Id).ToList();

                var attemptsDict = new Dictionary<string, (int SoLanThi, int SoLanGianLan)>();
                var gradedCounts = new Dictionary<int, int>();
                if (userIds.Any() && examIds.Any())
                {
                    var allUserExamAttempts = await _context.Baithis
                        .Where(b => b.IdTaiKhoan != null && userIds.Contains(b.IdTaiKhoan) 
                                 && b.IdDeThi != null && examIds.Contains(b.IdDeThi.Value)
                                 && (b.TrangThai == "Completed" || baithiIds.Contains(b.Id)))
                        .ToListAsync();

                    attemptsDict = allUserExamAttempts
                        .GroupBy(b => new { IdTaiKhoan = b.IdTaiKhoan ?? 0, IdDeThi = b.IdDeThi ?? 0 })
                        .ToDictionary(
                            g => $"{g.Key.IdTaiKhoan}_{g.Key.IdDeThi}",
                            g => (SoLanThi: g.Count(), SoLanGianLan: g.Sum(b => b.TongSoCanhBao ?? 0))
                        );
                }

                if (baithiIds.Any())
                {
                    gradedCounts = await _context.Chitietlambais
                        .Where(c => c.IdBaiThi != null && baithiIds.Contains(c.IdBaiThi.Value) && c.DiemDatDuoc != null)
                        .GroupBy(c => c.IdBaiThi!.Value)
                        .Select(g => new { BaiThiId = g.Key, GradedCount = g.Count() })
                        .ToDictionaryAsync(x => x.BaiThiId, x => x.GradedCount);
                }

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (baithiIds.Any())
                {
                    var chitietStats = await _context.Chitietlambais
                        .Where(c => c.IdBaiThi != null && baithiIds.Contains(c.IdBaiThi.Value))
                        .Select(c => new {
                            c.IdBaiThi,
                            IsGraded = c.DiemDatDuoc != null,
                            IsEssay = c.IdCauHoiNavigation != null 
                                && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null 
                                && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "Tự luận" 
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.TenLoai == "TuLuan")
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.IdBaiThi!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                mcqTotal: g.Count(c => !c.IsEssay),
                                mcqGraded: g.Count(c => !c.IsEssay && c.IsGraded),
                                essayTotal: g.Count(c => c.IsEssay),
                                essayGraded: g.Count(c => c.IsEssay && c.IsGraded)
                            ));
                }

                var results = baithis.Select(b =>
                {
                    var duration = (b.ThoiGianNop.HasValue && b.ThoiGianBatDau.HasValue)
                        ? (int)(b.ThoiGianNop.Value - b.ThoiGianBatDau.Value).TotalMinutes : 0;

                    int soLanThi = 1;
                    int soLanGianLan = 0;
                    int soLanThiLai = 0;

                    var key = $"{b.IdTaiKhoan ?? 0}_{b.IdDeThi ?? 0}";
                    if (attemptsDict.TryGetValue(key, out var counts))
                    {
                        soLanThi = counts.SoLanThi;
                        soLanGianLan = counts.SoLanGianLan;
                        soLanThiLai = Math.Max(0, counts.SoLanThi - 1);
                    }

                    int mcqTotal = 0, mcqGraded = 0, essayTotal = 0, essayGraded = 0;
                    if (statsDict.TryGetValue(b.Id, out var stats))
                    {
                        mcqTotal = stats.mcqTotal;
                        mcqGraded = stats.mcqGraded;
                        essayTotal = stats.essayTotal;
                        essayGraded = stats.essayGraded;
                    }

                    return new ExamResultDetailDto
                    {
                        BaiThiId = b.Id,
                        IdDeThi = b.IdDeThi,
                        Username = b.IdTaiKhoanNavigation?.TenDangNhap,
                        FullName = b.IdTaiKhoanNavigation?.HoTen,
                        MaNhanVien = b.IdTaiKhoanNavigation?.MaNhanVien,
                        KhoaPhong = b.IdTaiKhoanNavigation?.KhoaPhong,
                        ExamId = b.IdDeThi ?? 0,
                        MaDeThi = b.MaDeThi,
                        TenDeThi = b.IdDeThiNavigation?.TenDeThi,
                        ThoiGianBatDau = b.ThoiGianBatDau,
                        ThoiGianNop = b.ThoiGianNop,
                        DurationMinutes = duration,
                        TongDiem = b.SoCauDung,
                        SoCauDung = b.SoCauDung,
                        TongSoCau = b.TongSoCau,
                        TrangThai = b.TrangThai,
                        Pass = b.KyThiNavigation?.SoCauDungToiThieu != null 
                            ? (b.SoCauDung ?? 0) >= b.KyThiNavigation.SoCauDungToiThieu.Value 
                            : (b.IdDeThiNavigation?.SoCauDungToiThieu != null 
                                ? (b.SoCauDung ?? 0) >= b.IdDeThiNavigation.SoCauDungToiThieu.Value 
                                : true),
                        SoCauDungToiThieu = b.KyThiNavigation?.SoCauDungToiThieu ?? b.IdDeThiNavigation?.SoCauDungToiThieu,
                        SoCanhBao = b.TongSoCanhBao,
                        CongBoKetQua = b.CongBoRieng || (b.IdDeThiNavigation?.CongBoKetQua ?? false),
                        SoLanThi = soLanThi,
                        SoLanGianLan = soLanGianLan,
                        SoLanThiLai = soLanThiLai,
                        SoCauDaCham = gradedCounts.TryGetValue(b.Id, out var gc) ? gc : 0,
                        TongSoCauTracNghiem = mcqTotal,
                        SoCauTracNghiemDaCham = mcqGraded,
                        TongSoCauTuLuan = essayTotal,
                        SoCauTuLuanDaCham = essayGraded
                    };
                }).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = $"Lấy {results.Count} kết quả thi kỳ {kyThiId}",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results by KyThi {KyThiId}", kyThiId);
                return BaseResponseDto<List<ExamResultDetailDto>>.FailureResult("Lỗi khi lấy kết quả");
            }
        }
    }
}