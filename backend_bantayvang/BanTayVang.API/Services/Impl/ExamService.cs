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
        private readonly ICheatWarningRepository _canhbaoRepository;
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamService> _logger;

        public ExamService(
            IExamManagementService managementService,
            IExamSessionService sessionService,
            IExamSubmissionService submissionService,
            IExamSecurityService securityService,
            ICheatWarningRepository canhbaoRepository,
            IExamSubmissionRepository baithiRepository,
            BanTayVangDbContext context,
            ILogger<ExamService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _canhbaoRepository = canhbaoRepository ?? throw new ArgumentNullException(nameof(canhbaoRepository));
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Exam Management Operations (Delegated to IExamManagementService)

        public async Task<BaseResponseDto<ExamPaperDto>> CreateExamAsync(CreateExamPaperDto createDto, int createdBy)
        {
            return await _managementService.CreateExamAsync(createDto, createdBy);
        }

        public async Task<BaseResponseDto<ExamPaperDto>> GetExamByCodeAsync(string examPaperCode)
        {
            return await _managementService.GetExamByCodeAsync(examPaperCode);
        }

        public async Task<BaseResponseDto<List<ExamPaperDto>>> GetActiveExamsAsync()
        {
            return await _managementService.GetActiveExamsAsync();
        }

        public async Task<BaseResponseDto<List<ExamPaperDto>>> GetAllExamsAsync(string? status = null, string? department = null)
        {
            var result = await _managementService.GetAllExamsAsync(status);
            // Apply department filter in-memory if department provided
            if (result.Success && result.Data != null && !string.IsNullOrEmpty(department))
                result.Data = result.Data.Where(d => d.Department == department).ToList();
            return result;
        }

        public async Task<BaseResponseDto<ExamPaperDto>> UpdateExamAsync(int examId, UpdateExamPaperDto updateDto, int updatedBy)
        {
            return await _managementService.UpdateExamAsync(examId, updateDto, updatedBy);
        }

        public async Task<BaseResponseDto<ExamPaperDto>> UpdateExamStatusAsync(int examId, string status, int updatedBy)
        {
            return await _managementService.UpdateExamStatusAsync(examId, status, updatedBy);
        }

        #endregion

        #region Exam Session Operations (Delegated to IExamSessionService)

        public async Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa)
        {
            return await _managementService.DeleteExamAsync(examId, nguoiXoa);
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int taikhoanId)
        {
            return await _sessionService.StartExamAsync(startDto, taikhoanId);
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamQuestionsAsync(baithiId, taikhoanId);
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamProgressAsync(baithiId, taikhoanId);
        }

        #endregion

        #region Exam Submission Operations (Delegated to IExamSubmissionService)

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId)
        {
            return await _submissionService.SaveAnswerAsync(answerDto, taikhoanId);
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId)
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
        public async Task<BaseResponseDto<List<ExamSubmissionDto>>> GetMyResultsAsync(int taikhoanId)
        {
            try
            {
                var examSubmissions = await _examSubmissionRepository.GetByTaiKhoanAsync(taikhoanId);
                var completed = examSubmissions
                    .Where(b => b.Status == "Completed")
                    .OrderByDescending(b => b.SubmitTime ?? b.StartTime)
                    .GroupBy(b => b.ExamPaperCode ?? b.ExamPaperId?.ToString())
                    .Select(g => g.First())
                    .Select(b => {
                        var congBo = b.CongBoRieng || (b.IdDeThiNavigation?.IsResultPublished ?? false);
                        return new ExamSubmissionDto
                        {
                            Id = b.Id,
                            UserId = b.UserId ?? 0,
                            ExamPaperId = b.ExamPaperId ?? 0,
                            Status = b.Status,
                            SubmitTime = b.SubmitTime,
                            TotalScore = congBo ? (b.TotalQuestions > 0
                                ? Math.Round((b.CorrectAnswers ?? 0) * 10.0 / b.TotalQuestions!.Value, 2)
                                : b.TotalScore) : null,
                            CorrectAnswers = congBo ? b.CorrectAnswers : null,
                            TotalQuestions = b.TotalQuestions,
                            ExamPaperName = b.IdDeThiNavigation?.ExamPaperName,
                            ExamPaperCode = b.ExamPaperCode ?? b.IdDeThiNavigation?.ExamPaperCode,
                            StartTime = b.StartTime,
                            IsResultPublished = congBo,
                            Pass = congBo ? (b.TotalQuestions > 0
                                ? (b.CorrectAnswers ?? 0) * 10.0 / b.TotalQuestions!.Value >= 5
                                : (b.TotalScore ?? 0) >= 5) : false,
                        };
                    })
                    .ToList();

                return new BaseResponseDto<List<ExamSubmissionDto>>
                {
                    Success = true,
                    Data = completed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my results for user {UserId}", taikhoanId);
                return new BaseResponseDto<List<ExamSubmissionDto>>
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

                var examSubmission = await _examSubmissionRepository.GetByIdAsync(baithiId);
                if (examSubmission != null)
                {
                    if (examSubmission.Status == "Completed")
                    {
                        return new BaseResponseDto
                        {
                            Success = false,
                            Message = "Bài thi đã kết thúc, không thể ghi nhận cảnh báo"
                        };
                    }

                    if (loaiCanhBao == "FULLSCREEN_EXIT")
                    {
                        examSubmission.TongSoCanhBao = Math.Max(examSubmission.TongSoCanhBao ?? 0, 6);
                    }
                    else
                    {
                        examSubmission.TongSoCanhBao = (examSubmission.TongSoCanhBao ?? 0) + 1;
                    }
                    await _examSubmissionRepository.UpdateAsync(examSubmission);
                }

                // OWASP A09: Security Logging - Enhanced security event logging
                var canhbao = new CheatWarning
                {
                    ExamSubmissionId = baithiId,
                    LoaiCanhBao = loaiCanhBao,
                    Description = SanitizeInput(moTa), // OWASP A03: Injection prevention
                    ActionTime = DateTime.Now
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
                var examPaper = await _context.ExamPapers
                    .Include(d => d.KyThiNavigation)
                    .Include(d => d.ExamPaperQuestions)
                        .ThenInclude(dc => dc.IdCauHoiNavigation)
                            .ThenInclude(c => c.QuestionOptions)
                    .FirstOrDefaultAsync(d => d.Id == examId);

                if (examPaper == null)
                    return BaseResponseDto<ExamPreviewDto>.FailureResult("Không tìm thấy đề thi");

                // Shuffle mỗi lần preview để Reload tạo ra bản in khác nhau
                var rng = new Random();

                var preview = new ExamPreviewDto
                {
                    Id = examPaper.Id,
                    ExamPaperCode = examPaper.ExamPaperCode,
                    ExamPaperName = examPaper.ExamPaperName,
                    DurationMinutes = examPaper.DurationMinutes,
                    Status = examPaper.Status,
                    Department = examPaper.Department,
                    IsResultPublished = examPaper.IsResultPublished,
                    // Shuffle thứ tự câu hỏi
                    Questions = examPaper.ExamPaperQuestions
                        .OrderBy(_ => rng.Next())
                        .Select(dc => new QuestionPreviewDto
                        {
                            Id = dc.IdCauHoiNavigation?.Id ?? 0,
                            Content = dc.IdCauHoiNavigation?.Content,
                            ChuDe = null,
                            // Shuffle thứ tự đáp án
                            QuestionOptions = dc.IdCauHoiNavigation?.QuestionOptions
                                .OrderBy(_ => rng.Next())
                                .Select(lc => new ChoicePreviewDto
                                {
                                    Id = lc.Id,
                                    Content = lc.Content,
                                    IsCorrect = lc.IsCorrect
                                }).ToList() ?? new()
                        }).ToList()
                };

                // If no questions in ExamPaperQuestion (random pool exam), fetch from Department pool
                if (!preview.Questions.Any() && !string.IsNullOrEmpty(examPaper.Department))
                {
                    var allPoolQuestions = await _context.Questions
                        .Include(c => c.QuestionOptions)
                        .Where(c => c.Department == examPaper.Department && c.DaXoa != true)
                        .ToListAsync();

                    // Shuffle ngẫu nhiên mỗi lần preview (dùng lại rng ở trên), lọc trùng lặp theo nội dung câu hỏi
                    var poolQuestions = allPoolQuestions
                        .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                        .Select(g => g.First())
                        .OrderBy(_ => rng.Next())
                        .Take(examPaper.KyThiNavigation?.TotalQuestions ?? 10)
                        .ToList();

                    preview.Questions = poolQuestions.Select(c => new QuestionPreviewDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        ChuDe = null,
                        QuestionOptions = c.QuestionOptions.OrderBy(_ => rng.Next()).Select(lc => new ChoicePreviewDto
                        {
                            Id = lc.Id,
                            Content = lc.Content,
                            IsCorrect = lc.IsCorrect
                        }).ToList()
                    }).ToList();
                }

                return BaseResponseDto<ExamPreviewDto>.SuccessResult(preview, $"Đề thi có {preview.Questions.Count} câu hỏi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam preview {ExamId}", examId);
                return BaseResponseDto<ExamPreviewDto>.FailureResult("Lỗi khi lấy preview đề thi");
            }
        }

    }
}