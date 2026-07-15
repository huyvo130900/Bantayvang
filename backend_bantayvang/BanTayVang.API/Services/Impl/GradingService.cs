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

        public async Task<BaseResponseDto<ExamResultDetailDto>> GetResultDetailAsync(int examSubmissionId)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.QuestionOptions)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.IdLoaiCauHoiNavigation)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.IdLuaChonDaChonNavigation)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                if (examSubmission == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                var detail = MapToDetailDto(examSubmission);

                if (examSubmission.UserId != null && examSubmission.ExamPaperId != null)
                {
                    var attempts = await _context.ExamSubmissions
                        .Where(b => b.UserId == examSubmission.UserId && b.ExamPaperId == examSubmission.ExamPaperId && (b.Status == "Completed" || b.Id == examSubmission.Id))
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
                var allBaithis = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Where(b => b.ExamPaperId == examId && b.Status == "Completed")
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .OrderByDescending(b => b.SubmitTime)
                    .ToListAsync();

                // Group theo user, lấy bài thi gần nhất (điểm mới nhất)
                var latestPerUser = allBaithis
                    .GroupBy(b => b.UserId)
                    .Select(g => g.First())
                    .ToList();

                // Đếm số lần thi và tổng gian lận mỗi user
                var countPerUser = allBaithis
                    .GroupBy(b => b.UserId)
                    .ToDictionary(
                        g => g.Key ?? 0,
                        g => new {
                            SoLanThi = g.Count(),
                            SoLanGianLan = g.Sum(b => b.TongSoCanhBao ?? 0)
                        });

                var latestPerUserIds = latestPerUser.Select(b => b.Id).ToList();
                var gradedCounts = await _context.SubmissionDetails
                    .Where(c => c.ExamSubmissionId != null && latestPerUserIds.Contains(c.ExamSubmissionId.Value) && c.ScoreObtained != null)
                    .GroupBy(c => c.ExamSubmissionId!.Value)
                    .Select(g => new { ExamSubmissionId = g.Key, GradedCount = g.Count() })
                    .ToDictionaryAsync(x => x.ExamSubmissionId, x => x.GradedCount);

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (latestPerUserIds.Any())
                {
                    var chitietStats = await _context.SubmissionDetails
                        .Where(c => c.ExamSubmissionId != null && latestPerUserIds.Contains(c.ExamSubmissionId.Value))
                        .Select(c => new {
                            c.ExamSubmissionId,
                            IsGraded = c.ScoreObtained != null,
                            IsEssay = c.IdCauHoiNavigation != null 
                                && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null 
                                && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "Tự luận" 
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "TuLuan"
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "TL")
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.ExamSubmissionId!.Value)
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
                    if (countPerUser.TryGetValue(b.UserId ?? 0, out var counts))
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

        public async Task<BaseResponseDto<ExamResultDetailDto>> RegradeAsync(int examSubmissionId)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.QuestionOptions)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.IdCauHoiNavigation)
                            .ThenInclude(ch => ch!.IdLoaiCauHoiNavigation)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                if (examSubmission == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                int correctCount = 0;
                double totalScore = 0;

                // Group answers by question to support multiple-choice
                var answersByQuestion = examSubmission.SubmissionDetails
                    .Where(c => c.QuestionId.HasValue)
                    .GroupBy(c => c.QuestionId!.Value);

                foreach (var group in answersByQuestion)
                {
                    var question = group.First().IdCauHoiNavigation;
                    if (question == null) continue;

                    var correctChoiceIds = question.QuestionOptions
                        .Where(l => l.IsCorrect == true)
                        .Select(l => l.Id)
                        .ToHashSet();

                    var categoryName = question.IdLoaiCauHoiNavigation?.CategoryName;
                            var moTa = question.IdLoaiCauHoiNavigation?.Description;
                    var isEssay = categoryName == "Tự luận" || categoryName == "TuLuan" || categoryName == "TL" || correctChoiceIds.Count == 0;

                    if (isEssay)
                    {
                        // For essay, preserve existing ScoreObtained which is manually graded
                        var score = group.FirstOrDefault()?.ScoreObtained ?? 0;
                        if (score >= 1)
                        {
                            correctCount++;
                            totalScore += 1;
                        }
                    }
                    else
                    {
                        var userChoiceIds = group
                            .Where(c => c.SelectedOptionId.HasValue)
                            .Select(c => c.SelectedOptionId!.Value)
                            .ToHashSet();

                        bool isFullyCorrect = correctChoiceIds.Count > 0 
                            && correctChoiceIds.SetEquals(userChoiceIds);

                        foreach (var ct in group)
                        {
                            if (isFullyCorrect)
                            {
                                ct.ScoreObtained = 1.0 / Math.Max(1, group.Count());
                            }
                            else
                            {
                                ct.ScoreObtained = 0;
                            }
                        }

                        if (isFullyCorrect)
                        {
                            correctCount++;
                            totalScore += 1;
                        }
                    }
                }

                examSubmission.CorrectAnswers = correctCount;
                var tongSoCau = examSubmission.TotalQuestions ?? answersByQuestion.Count();
                examSubmission.TotalScore = correctCount;

                await _context.SaveChangesAsync();

                return await GetResultDetailAsync(examSubmissionId);
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
                var chitiet = await _context.SubmissionDetails
                    .Include(c => c.IdBaiThiNavigation)
                    .Include(c => c.IdCauHoiNavigation)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChiTietLamBaiId);

                if (chitiet == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy chi tiết bài làm" };

                // Đánh dấu Đúng/Sai: ScoreObtained = 1 nếu đúng, = 0 nếu sai, = null nếu chấm lại
                chitiet.ScoreObtained = dto.IsCorrect.HasValue ? (dto.IsCorrect.Value ? 1.0 : 0.0) : (double?)null;
                if (!string.IsNullOrEmpty(dto.NhanXet))
                    chitiet.CauTraLoiTuLuan = chitiet.CauTraLoiTuLuan; // giữ nguyên nội dung

                await _context.SaveChangesAsync();

                // Tính lại tổng số câu đúng cho bài thi
                if (chitiet.ExamSubmissionId.HasValue)
                {
                    var examSubmission = await _context.ExamSubmissions
                        .Include(b => b.SubmissionDetails)
                            .ThenInclude(c => c.IdCauHoiNavigation)
                        .FirstOrDefaultAsync(b => b.Id == chitiet.ExamSubmissionId.Value);
                    
                    if (examSubmission != null)
                    {
                        // Tính số câu đúng: trắc nghiệm (isCorrect = là câu chọn đúng) + tự luận (ScoreObtained == 1)
                        int correctAnswers = 0;
                        foreach (var ct in examSubmission.SubmissionDetails)
                        {
                            var categoryName = ct.IdCauHoiNavigation?.IdLoaiCauHoiNavigation?.CategoryName;
                            var moTa = ct.IdCauHoiNavigation?.IdLoaiCauHoiNavigation?.Description;
                            bool isTuLuan = categoryName == "Tự luận" || categoryName == "TuLuan" || categoryName == "TL";
                            if (isTuLuan)
                            {
                                if (ct.ScoreObtained == 1.0) correctAnswers++;
                            }
                            else
                            {
                                // Trắc nghiệm: đã được chấm tự động, ScoreObtained > 0 nghĩa là đúng
                                if ((ct.ScoreObtained ?? 0) > 0) correctAnswers++;
                            }
                        }
                        examSubmission.CorrectAnswers = correctAnswers;
                        examSubmission.TotalScore = correctAnswers;
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
                var examSubmissions = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Where(b => b.ExamPaperId == examId && b.Status == "Completed")
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .OrderByDescending(b => b.TotalScore)
                    .ThenBy(b => b.SubmitTime) // tie-break by submission time
                    .Take(top)
                    .ToListAsync();

                var results = examSubmissions.Select(b => MapToDetailDtoSummary(b)).ToList();

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
                var ungraded = await _context.ExamSubmissions
                    .Where(b => b.Status == "Completed" && (b.TotalScore == null || b.CorrectAnswers == null))
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

        private ExamResultDetailDto MapToDetailDto(ExamSubmission examSubmission)
        {
            int? durationMinutes = null;
            int? durationSeconds = null;
            if (examSubmission.StartTime.HasValue && examSubmission.SubmitTime.HasValue)
            {
                var diff = examSubmission.SubmitTime.Value - examSubmission.StartTime.Value;
                durationMinutes = (int)diff.TotalMinutes;
                durationSeconds = (int)diff.TotalSeconds;
            }

            var detail = new ExamResultDetailDto
            {
                ExamSubmissionId = examSubmission.Id,
                UserId = examSubmission.UserId,
                Username = examSubmission.IdTaiKhoanNavigation?.Username,
                FullName = examSubmission.IdTaiKhoanNavigation?.FullName,
                EmployeeCode = examSubmission.IdTaiKhoanNavigation?.EmployeeCode,
                Department = examSubmission.IdTaiKhoanNavigation?.Department,
                ExamId = examSubmission.ExamPaperId ?? 0,
                ExamPaperId = examSubmission.ExamPaperId,
                ExamPaperCode = examSubmission.IdDeThiNavigation?.ExamPaperCode,
                ExamPaperName = examSubmission.IdDeThiNavigation?.ExamPaperName,
                StartTime = examSubmission.StartTime,
                SubmitTime = examSubmission.SubmitTime,
                DurationMinutes = durationMinutes,
                DurationSeconds = durationSeconds,
                TotalScore = examSubmission.TotalQuestions.HasValue && examSubmission.TotalQuestions.Value > 0
                    ? Math.Round((double)(examSubmission.CorrectAnswers ?? 0) / examSubmission.TotalQuestions.Value * 10, 2)
                    : 0,
                CorrectAnswers = examSubmission.CorrectAnswers,
                TotalQuestions = examSubmission.TotalQuestions,
                Status = examSubmission.Status,
                Pass = examSubmission.KyThiNavigation?.MinPassQuestions != null 
                    ? (examSubmission.CorrectAnswers ?? 0) >= examSubmission.KyThiNavigation.MinPassQuestions.Value 
                    : (examSubmission.IdDeThiNavigation?.MinPassQuestions != null 
                        ? (examSubmission.CorrectAnswers ?? 0) >= examSubmission.IdDeThiNavigation.MinPassQuestions.Value 
                        : true),
                MinPassQuestions = examSubmission.KyThiNavigation?.MinPassQuestions ?? examSubmission.IdDeThiNavigation?.MinPassQuestions,
                SoCanhBao = examSubmission.TongSoCanhBao,
                IsResultPublished = examSubmission.CongBoRieng || (examSubmission.IdDeThiNavigation?.IsResultPublished ?? false),
                Answers = new List<AnswerDetailDto>()
            };

            int mcqTotal = 0;
            int mcqGraded = 0;
            int essayTotal = 0;
            int essayGraded = 0;

            var answersByQuestion = examSubmission.SubmissionDetails
                .Where(c => c.QuestionId.HasValue)
                .GroupBy(c => c.QuestionId!.Value);

            foreach (var group in answersByQuestion)
            {
                var question = group.First().IdCauHoiNavigation;
                if (question == null) continue;

                var categoryName = question.IdLoaiCauHoiNavigation?.CategoryName;
                            var moTa = question.IdLoaiCauHoiNavigation?.Description;
                bool isEssay = categoryName == "Tự luận" || categoryName == "TuLuan" || categoryName == "TL";

                if (isEssay)
                {
                    essayTotal++;
                    if (group.Any(c => c.ScoreObtained != null)) essayGraded++;
                }
                else
                {
                    mcqTotal++;
                    if (group.Any(c => c.ScoreObtained != null)) mcqGraded++;
                }

                // Check correctness (so khớp tập hợp đáp án đúng và lựa chọn của user)
                var correctChoiceIds = question.QuestionOptions
                    .Where(l => l.IsCorrect == true)
                    .Select(l => l.Id)
                    .ToHashSet();

                var userChoiceIds = group
                    .Where(c => c.SelectedOptionId.HasValue)
                    .Select(c => c.SelectedOptionId!.Value)
                    .ToHashSet();

                bool isFullyCorrect = false;
                if (isEssay)
                {
                    var score = group.FirstOrDefault()?.ScoreObtained ?? 0;
                    isFullyCorrect = score >= 1;
                }
                else
                {
                    isFullyCorrect = correctChoiceIds.Count > 0 && correctChoiceIds.SetEquals(userChoiceIds);
                }

                // Tạo chuỗi hiển thị cho các lựa chọn của thí sinh
                var userChoiceTexts = group
                    .Where(c => c.SelectedOptionId.HasValue && c.IdLuaChonDaChonNavigation != null)
                    .OrderBy(c => c.IdLuaChonDaChonNavigation!.OrderIndex)
                    .Select(c => c.IdLuaChonDaChonNavigation!.Content)
                    .ToList();
                var userChoiceText = userChoiceTexts.Any() ? string.Join(", ", userChoiceTexts) : "";

                // Tạo chuỗi hiển thị cho các đáp án đúng thực tế
                var correctChoiceTexts = question.QuestionOptions
                    .Where(l => l.IsCorrect == true)
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => l.Content)
                    .ToList();
                var correctChoiceText = correctChoiceTexts.Any() ? string.Join(", ", correctChoiceTexts) : "";

                var firstCt = group.First();
                var firstCorrectChoice = question.QuestionOptions.FirstOrDefault(l => l.IsCorrect == true);

                detail.Answers.Add(new AnswerDetailDto
                {
                    QuestionId = question.Id,
                    NoiDungCauHoi = question.Content,
                    QuestionCategory = question.IdLoaiCauHoiNavigation?.CategoryName,
                    SelectedOptionId = firstCt.SelectedOptionId, // Fallback
                    NoiDungDapAn = isEssay ? firstCt.CauTraLoiTuLuan : userChoiceText,
                    CauTraLoiTuLuan = firstCt.CauTraLoiTuLuan,
                    IsCorrect = isFullyCorrect,
                    ScoreObtained = group.All(c => c.ScoreObtained == null) ? (double?)null : group.Sum(c => c.ScoreObtained ?? 0),
                    IdLuaChonDung = firstCorrectChoice?.Id, // Fallback
                    NoiDungDapAnDung = correctChoiceText,
                    ChiTietLamBaiId = firstCt.Id
                });
            }

            detail.SoCauDaCham = answersByQuestion.Count(g => g.Any(c => c.ScoreObtained != null));
            detail.TongSoCauTracNghiem = mcqTotal;
            detail.SoCauTracNghiemDaCham = mcqGraded;
            detail.TongSoCauTuLuan = essayTotal;
            detail.SoCauTuLuanDaCham = essayGraded;

            return detail;
        }

        private ExamResultDetailDto MapToDetailDtoSummary(ExamSubmission examSubmission)
        {
            int? duration = null;
            if (examSubmission.StartTime.HasValue && examSubmission.SubmitTime.HasValue)
            {
                duration = (int)(examSubmission.SubmitTime.Value - examSubmission.StartTime.Value).TotalMinutes;
            }

            return new ExamResultDetailDto
            {
                ExamSubmissionId = examSubmission.Id,
                UserId = examSubmission.UserId,
                Username = examSubmission.IdTaiKhoanNavigation?.Username,
                FullName = examSubmission.IdTaiKhoanNavigation?.FullName,
                EmployeeCode = examSubmission.IdTaiKhoanNavigation?.EmployeeCode,
                Department = examSubmission.IdTaiKhoanNavigation?.Department,
                ExamId = examSubmission.ExamPaperId ?? 0,
                ExamPaperCode = examSubmission.ExamPaperCode ?? examSubmission.IdDeThiNavigation?.ExamPaperCode,
                ExamPaperName = examSubmission.IdDeThiNavigation?.ExamPaperName,
                StartTime = examSubmission.StartTime,
                SubmitTime = examSubmission.SubmitTime,
                DurationMinutes = duration,
                TotalScore = examSubmission.TotalQuestions.HasValue && examSubmission.TotalQuestions.Value > 0
                    ? Math.Round((double)(examSubmission.CorrectAnswers ?? 0) / examSubmission.TotalQuestions.Value * 10, 2)
                    : 0,
                CorrectAnswers = examSubmission.CorrectAnswers,
                TotalQuestions = examSubmission.TotalQuestions,
                Status = examSubmission.Status,
                Pass = examSubmission.KyThiNavigation?.MinPassQuestions != null 
                    ? (examSubmission.CorrectAnswers ?? 0) >= examSubmission.KyThiNavigation.MinPassQuestions.Value 
                    : (examSubmission.IdDeThiNavigation?.MinPassQuestions != null 
                        ? (examSubmission.CorrectAnswers ?? 0) >= examSubmission.IdDeThiNavigation.MinPassQuestions.Value 
                        : true),
                MinPassQuestions = examSubmission.KyThiNavigation?.MinPassQuestions ?? examSubmission.IdDeThiNavigation?.MinPassQuestions,
                SoCanhBao = examSubmission.TongSoCanhBao,
                IsResultPublished = examSubmission.CongBoRieng || (examSubmission.IdDeThiNavigation?.IsResultPublished ?? false),
                DanhGiaKhoa = examSubmission.DanhGiaKhoa,
            };
        }


        /// <summary>
        /// Quản lý khoa đánh giá / nhận xét bài thi của thí sinh
        /// </summary>
        public async Task<BaseResponseDto> EvaluateCandidateAsync(int examSubmissionId, string evaluation)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions.FindAsync(examSubmissionId);
                if (examSubmission == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };

                examSubmission.DanhGiaKhoa = evaluation;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã lưu đánh giá" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving DanhGiaKhoa for examSubmission {ExamSubmissionId}", examSubmissionId);
                return new BaseResponseDto { Success = false, Message = "Lỗi khi lưu đánh giá" };
            }
        }

        #endregion

        /// <summary>
        /// Lấy kết quả thi theo Kỳ thi
        /// </summary>
        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByExamCampaignAsync(int examCampaignId)
        {
            try
            {
                var examSubmissions = await _context.ExamSubmissions
                    .IgnoreQueryFilters()
                    .Include(b => b.IdTaiKhoanNavigation)
                    .Include(b => b.IdDeThiNavigation)
                    .Include(b => b.KyThiNavigation)
                    .Where(b => b.ExamCampaignId == examCampaignId)
                    .OrderByDescending(b => b.TotalScore)
                    .ToListAsync();

                var userIds = examSubmissions.Select(b => b.UserId).Distinct().ToList();
                var examIds = examSubmissions.Select(b => b.ExamPaperId).Distinct().ToList();
                var baithiIds = examSubmissions.Select(b => b.Id).ToList();

                var attemptsDict = new Dictionary<string, (int SoLanThi, int SoLanGianLan)>();
                var gradedCounts = new Dictionary<int, int>();
                if (userIds.Any() && examIds.Any())
                {
                    var allUserExamAttempts = await _context.ExamSubmissions
                        .Where(b => b.UserId != null && userIds.Contains(b.UserId) 
                                 && b.ExamPaperId != null && examIds.Contains(b.ExamPaperId.Value)
                                 && (b.Status == "Completed" || baithiIds.Contains(b.Id)))
                        .ToListAsync();

                    attemptsDict = allUserExamAttempts
                        .GroupBy(b => new { UserId = b.UserId ?? 0, ExamPaperId = b.ExamPaperId ?? 0 })
                        .ToDictionary(
                            g => $"{g.Key.UserId}_{g.Key.ExamPaperId}",
                            g => (SoLanThi: g.Count(), SoLanGianLan: g.Sum(b => b.TongSoCanhBao ?? 0))
                        );
                }

                if (baithiIds.Any())
                {
                    gradedCounts = await _context.SubmissionDetails
                        .Where(c => c.ExamSubmissionId != null && baithiIds.Contains(c.ExamSubmissionId.Value) && c.ScoreObtained != null)
                        .GroupBy(c => c.ExamSubmissionId!.Value)
                        .Select(g => new { ExamSubmissionId = g.Key, GradedCount = g.Count() })
                        .ToDictionaryAsync(x => x.ExamSubmissionId, x => x.GradedCount);
                }

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (baithiIds.Any())
                {
                    var chitietStats = await _context.SubmissionDetails
                        .Where(c => c.ExamSubmissionId != null && baithiIds.Contains(c.ExamSubmissionId.Value))
                        .Select(c => new {
                            c.ExamSubmissionId,
                            IsGraded = c.ScoreObtained != null,
                            IsEssay = c.IdCauHoiNavigation != null 
                                && c.IdCauHoiNavigation.IdLoaiCauHoiNavigation != null 
                                && (c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "Tự luận" 
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "TuLuan"
                                    || c.IdCauHoiNavigation.IdLoaiCauHoiNavigation.CategoryName == "TL")
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.ExamSubmissionId!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                mcqTotal: g.Count(c => !c.IsEssay),
                                mcqGraded: g.Count(c => !c.IsEssay && c.IsGraded),
                                essayTotal: g.Count(c => c.IsEssay),
                                essayGraded: g.Count(c => c.IsEssay && c.IsGraded)
                            ));
                }

                var results = examSubmissions.Select(b =>
                {
                    var duration = (b.SubmitTime.HasValue && b.StartTime.HasValue)
                        ? (int)(b.SubmitTime.Value - b.StartTime.Value).TotalMinutes : 0;

                    int soLanThi = 1;
                    int soLanGianLan = 0;
                    int soLanThiLai = 0;

                    var key = $"{b.UserId ?? 0}_{b.ExamPaperId ?? 0}";
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
                        ExamSubmissionId = b.Id,
                        ExamPaperId = b.ExamPaperId,
                        Username = b.IdTaiKhoanNavigation?.Username,
                        FullName = b.IdTaiKhoanNavigation?.FullName,
                        EmployeeCode = b.IdTaiKhoanNavigation?.EmployeeCode,
                        Department = b.IdTaiKhoanNavigation?.Department,
                        ExamId = b.ExamPaperId ?? 0,
                        ExamPaperCode = b.ExamPaperCode,
                        ExamPaperName = b.IdDeThiNavigation?.ExamPaperName,
                        StartTime = b.StartTime,
                        SubmitTime = b.SubmitTime,
                        DurationMinutes = duration,
                        TotalScore = b.TotalQuestions.HasValue && b.TotalQuestions.Value > 0
                            ? Math.Round((double)(b.CorrectAnswers ?? 0) / b.TotalQuestions.Value * 10, 2)
                            : 0,
                        CorrectAnswers = b.CorrectAnswers,
                        TotalQuestions = b.TotalQuestions,
                        Status = b.Status,
                        Pass = b.KyThiNavigation?.MinPassQuestions != null 
                            ? (b.CorrectAnswers ?? 0) >= b.KyThiNavigation.MinPassQuestions.Value 
                            : (b.IdDeThiNavigation?.MinPassQuestions != null 
                                ? (b.CorrectAnswers ?? 0) >= b.IdDeThiNavigation.MinPassQuestions.Value 
                                : true),
                        MinPassQuestions = b.KyThiNavigation?.MinPassQuestions ?? b.IdDeThiNavigation?.MinPassQuestions,
                        SoCanhBao = b.TongSoCanhBao,
                        IsResultPublished = b.CongBoRieng || (b.IdDeThiNavigation?.IsResultPublished ?? false),
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
                    Message = $"Lấy {results.Count} kết quả thi kỳ {examCampaignId}",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results by ExamCampaign {ExamCampaignId}", examCampaignId);
                return BaseResponseDto<List<ExamResultDetailDto>>.FailureResult("Lỗi khi lấy kết quả");
            }
        }
    }
}



