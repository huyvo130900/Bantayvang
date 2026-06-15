using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Main exam service implementing SOLID principles with segregated responsibilities
    /// Acts as a facade for specialized exam services
    /// </summary>
    public class ExamService : IExamService
    {
        private readonly IExamManagementService _managementService;
        private readonly IExamSessionService _sessionService;
        private readonly IExamSubmissionService _submissionService;
        private readonly IExamSecurityService _securityService;
        private readonly ICanhbaogianlanRepository _canhbaoRepository;
        private readonly IBaithiRepository _baithiRepository;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamService> _logger;

        public ExamService(
            IExamManagementService managementService,
            IExamSessionService sessionService,
            IExamSubmissionService submissionService,
            IExamSecurityService securityService,
            ICanhbaogianlanRepository canhbaoRepository,
            IBaithiRepository baithiRepository,
            BanTayVangDbContext context,
            ILogger<ExamService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _canhbaoRepository = canhbaoRepository ?? throw new ArgumentNullException(nameof(canhbaoRepository));
            _baithiRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Exam Management Operations (Delegated to IExamManagementService)

        public async Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao)
        {
            return await _managementService.CreateExamAsync(createDto, nguoiTao);
        }

        public async Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi)
        {
            return await _managementService.GetExamByCodeAsync(maDeThi);
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync()
        {
            return await _managementService.GetActiveExamsAsync();
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetAllExamsAsync(string? trangThai = null, string? khoaPhong = null)
        {
            var result = await _managementService.GetAllExamsAsync(trangThai);
            // Apply department filter in-memory if khoaPhong provided
            if (result.Success && result.Data != null && !string.IsNullOrEmpty(khoaPhong))
                result.Data = result.Data.Where(d => d.KhoaPhong == khoaPhong).ToList();
            return result;
        }

        public async Task<BaseResponseDto<DethiDto>> UpdateExamAsync(int examId, UpdateDethiDto updateDto, int nguoiCapNhat)
        {
            return await _managementService.UpdateExamAsync(examId, updateDto, nguoiCapNhat);
        }

        public async Task<BaseResponseDto<DethiDto>> UpdateExamStatusAsync(int examId, string trangThai, int nguoiCapNhat)
        {
            return await _managementService.UpdateExamStatusAsync(examId, trangThai, nguoiCapNhat);
        }

        #endregion

        #region Exam Session Operations (Delegated to IExamSessionService)

        public async Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa)
        {
            return await _managementService.DeleteExamAsync(examId, nguoiXoa);
        }

        public async Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId)
        {
            return await _sessionService.StartExamAsync(startDto, taikhoanId);
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamQuestionsAsync(baithiId, taikhoanId);
        }

        public async Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamProgressAsync(baithiId, taikhoanId);
        }

        #endregion

        #region Exam Submission Operations (Delegated to IExamSubmissionService)

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId)
        {
            return await _submissionService.SaveAnswerAsync(answerDto, taikhoanId);
        }

        public async Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId)
        {
            return await _submissionService.SubmitExamAsync(submitDto, taikhoanId);
        }

        public async Task<BaseResponseDto> AutoSubmitExpiredExamsAsync()
        {
            return await _submissionService.AutoSubmitExpiredExamsAsync();
        }

        #endregion

        #region My Results (THÊM MỚI)

        /// <summary>
        /// Lấy danh sách bài thi đã hoàn thành của user hiện tại
        /// </summary>
        public async Task<BaseResponseDto<List<BaithiDto>>> GetMyResultsAsync(int taikhoanId)
        {
            try
            {
                var baithis = await _baithiRepository.GetByTaiKhoanAsync(taikhoanId);
                var completed = baithis
                    .Where(b => b.TrangThai == "Completed")
                    .OrderByDescending(b => b.ThoiGianNop ?? b.ThoiGianBatDau)
                    .GroupBy(b => b.MaDeThi ?? b.IdDeThi?.ToString())
                    .Select(g => g.First())
                    .Select(b => {
                        var congBo = b.CongBoRieng || (b.IdDeThiNavigation?.CongBoKetQua ?? false);
                        return new BaithiDto
                        {
                            Id = b.Id,
                            IdTaiKhoan = b.IdTaiKhoan ?? 0,
                            IdDeThi = b.IdDeThi ?? 0,
                            TrangThai = b.TrangThai,
                            ThoiGianNop = b.ThoiGianNop,
                            TongDiem = congBo ? (b.TongSoCau > 0
                                ? Math.Round((b.SoCauDung ?? 0) * 10.0 / b.TongSoCau!.Value, 2)
                                : b.TongDiem) : null,
                            SoCauDung = congBo ? b.SoCauDung : null,
                            TongSoCau = b.TongSoCau,
                            TenDeThi = b.IdDeThiNavigation?.TenDeThi,
                            MaDeThi = b.MaDeThi ?? b.IdDeThiNavigation?.MaDeThi,
                            ThoiGianBatDau = b.ThoiGianBatDau,
                            CongBoKetQua = congBo,
                            Pass = congBo ? (b.TongSoCau > 0
                                ? (b.SoCauDung ?? 0) * 10.0 / b.TongSoCau!.Value >= 5
                                : (b.TongDiem ?? 0) >= 5) : false,
                        };
                    })
                    .ToList();

                return new BaseResponseDto<List<BaithiDto>>
                {
                    Success = true,
                    Data = completed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my results for user {UserId}", taikhoanId);
                return new BaseResponseDto<List<BaithiDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy kết quả bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        #endregion

        #region Security Operations (Enhanced with OWASP compliance)

        public async Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa)
        {
            try
            {
                _logger.LogWarning("Suspicious activity detected - Session: {SessionId}, Type: {Type}, Description: {Description}",
                    baithiId, loaiCanhBao, moTa);

                var baithi = await _baithiRepository.GetByIdAsync(baithiId);
                if (baithi != null)
                {
                    if (baithi.TrangThai == "Completed")
                    {
                        return new BaseResponseDto
                        {
                            Success = false,
                            Message = "Bài thi đã kết thúc, không thể ghi nhận cảnh báo"
                        };
                    }

                    if (loaiCanhBao == "FULLSCREEN_EXIT")
                    {
                        baithi.TongSoCanhBao = Math.Max(baithi.TongSoCanhBao ?? 0, 6);
                    }
                    else
                    {
                        baithi.TongSoCanhBao = (baithi.TongSoCanhBao ?? 0) + 1;
                    }
                    await _baithiRepository.UpdateAsync(baithi);
                }

                // OWASP A09: Security Logging - Enhanced security event logging
                var canhbao = new Canhbaogianlan
                {
                    IdBaiThi = baithiId,
                    LoaiCanhBao = loaiCanhBao,
                    MoTa = SanitizeInput(moTa), // OWASP A03: Injection prevention
                    ThoiGian = DateTime.Now
                };

                await _canhbaoRepository.AddAsync(canhbao);

                // Log to security service for centralized monitoring
                await _securityService.LogSecurityEventAsync(
                    $"SUSPICIOUS_ACTIVITY_{loaiCanhBao}",
                    moTa,
                    null, // Will be extracted from session
                    DetermineSeverityLevel(loaiCanhBao));

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đã ghi nhận cảnh báo bảo mật"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging suspicious activity for session {SessionId}", baithiId);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi ghi nhận cảnh báo",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<int>> GetWarningCountAsync(int baithiId)
        {
            try
            {
                var count = await _canhbaoRepository.GetTotalWarningsAsync(baithiId);

                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = "Lấy số lượng cảnh báo thành công",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warning count for session {SessionId}", baithiId);

                return new BaseResponseDto<int>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy số lượng cảnh báo",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Determine security event severity based on warning type
        /// </summary>
        private string DetermineSeverityLevel(string loaiCanhBao)
        {
            return loaiCanhBao.ToUpperInvariant() switch
            {
                "TAB_SWITCH" => "Medium",
                "COPY_PASTE" => "High",
                "RIGHT_CLICK" => "Low",
                "MULTIPLE_TABS" => "High",
                "BROWSER_FOCUS_LOST" => "Medium",
                "SUSPICIOUS_KEYBOARD" => "High",
                "SCREEN_CAPTURE" => "Critical",
                _ => "Medium"
            };
        }

        /// <summary>
        /// OWASP A03: Injection - Sanitize input to prevent XSS and injection attacks
        /// </summary>
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace("<script", "&lt;script")
                .Replace("</script>", "&lt;/script&gt;")
                .Replace("javascript:", "")
                .Replace("vbscript:", "")
                .Replace("onload=", "")
                .Replace("onerror=", "")
                .Replace("onclick=", "")
                .Trim();
        }

        #endregion
        public async Task<BaseResponseDto<ExamPreviewDto>> GetExamPreviewAsync(int examId)
        {
            try
            {
                var dethi = await _context.Dethis
                    .Include(d => d.KyThiNavigation)
                    .Include(d => d.DethiCauhois)
                        .ThenInclude(dc => dc.IdCauHoiNavigation)
                            .ThenInclude(c => c.Luachons)
                    .FirstOrDefaultAsync(d => d.Id == examId);

                if (dethi == null)
                    return BaseResponseDto<ExamPreviewDto>.FailureResult("Không tìm thấy đề thi");

                // Shuffle mỗi lần preview để Reload tạo ra bản in khác nhau
                var rng = new Random();

                var preview = new ExamPreviewDto
                {
                    Id = dethi.Id,
                    MaDeThi = dethi.MaDeThi,
                    TenDeThi = dethi.TenDeThi,
                    ThoiGianLamBai = dethi.ThoiGianLamBai,
                    TrangThai = dethi.TrangThai,
                    KhoaPhong = dethi.KhoaPhong,
                    CongBoKetQua = dethi.CongBoKetQua,
                    // Shuffle thứ tự câu hỏi
                    CauHois = dethi.DethiCauhois
                        .OrderBy(_ => rng.Next())
                        .Select(dc => new QuestionPreviewDto
                        {
                            Id = dc.IdCauHoiNavigation?.Id ?? 0,
                            NoiDung = dc.IdCauHoiNavigation?.NoiDung,
                            ChuDe = null,
                            // Shuffle thứ tự đáp án
                            Luachons = dc.IdCauHoiNavigation?.Luachons
                                .OrderBy(_ => rng.Next())
                                .Select(lc => new ChoicePreviewDto
                                {
                                    Id = lc.Id,
                                    NoiDung = lc.NoiDung,
                                    LaDapAnDung = lc.LaDapAnDung
                                }).ToList() ?? new()
                        }).ToList()
                };

                // If no questions in DethiCauhoi (random pool exam), fetch from KhoaPhong pool
                if (!preview.CauHois.Any() && !string.IsNullOrEmpty(dethi.KhoaPhong))
                {
                    var allPoolQuestions = await _context.Cauhois
                        .Include(c => c.Luachons)
                        .Where(c => c.KhoaPhong == dethi.KhoaPhong && c.DaXoa != true)
                        .ToListAsync();

                    // Shuffle ngẫu nhiên mỗi lần preview (dùng lại rng ở trên)
                    var poolQuestions = allPoolQuestions
                        .OrderBy(_ => rng.Next())
                        .Take(dethi.KyThiNavigation?.TongSoCauHoi ?? 10)
                        .ToList();

                    preview.CauHois = poolQuestions.Select(c => new QuestionPreviewDto
                    {
                        Id = c.Id,
                        NoiDung = c.NoiDung,
                        ChuDe = null,
                        Luachons = c.Luachons.OrderBy(_ => rng.Next()).Select(lc => new ChoicePreviewDto
                        {
                            Id = lc.Id,
                            NoiDung = lc.NoiDung,
                            LaDapAnDung = lc.LaDapAnDung
                        }).ToList()
                    }).ToList();
                }

                return BaseResponseDto<ExamPreviewDto>.SuccessResult(preview, $"Đề thi có {preview.CauHois.Count} câu hỏi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam preview {ExamId}", examId);
                return BaseResponseDto<ExamPreviewDto>.FailureResult("Lỗi khi lấy preview đề thi");
            }
        }

    }
}