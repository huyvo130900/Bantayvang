using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl.Exams
{
    /// <summary>
    /// Exam session service implementation following SOLID principles and OWASP security
    /// </summary>
    public class ExamSessionService : IExamSessionService
    {
        private readonly IExamPaperRepository _examPaperRepository;
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly ISubmissionDetailRepository _chitietRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamSessionService> _logger;

        public ExamSessionService(
            IExamPaperRepository dethiRepository,
            IExamSubmissionRepository baithiRepository,
            ISubmissionDetailRepository chitietRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamSessionService> logger)
        {
            _examPaperRepository = dethiRepository ?? throw new ArgumentNullException(nameof(dethiRepository));
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _chitietRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting exam session for user {UserId} with exam code {ExamCode}, KyThiId {KyThiId}", taikhoanId, startDto.ExamPaperCode, startDto.KyThiId);

                // OWASP A03: Injection - Input validation
                var validationResult = await _validationService.ValidateStartExamAsync(startDto, taikhoanId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_START_VALIDATION_FAILED",
                        $"User {taikhoanId} failed validation for exam {startDto.ExamPaperCode}",
                        taikhoanId, "Medium", cancellationToken);
                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = validationResult.Message,
                        Errors = validationResult.Errors
                    };
                }

                // Resolve exam paper (ExamPaper)
                // Rule: Thí sinh làm các đề khác nhau khi thi lại. Làm hết các đề mới lặp lại.
                ExamPaper? examPaper = null;
                if (startDto.KyThiId.HasValue)
                {
                    examPaper = await _examPaperRepository.ResolveExamForCandidateAsync(startDto.KyThiId.Value, taikhoanId, cancellationToken);
                }

                // Fallback to ExamPaperCode if not resolved via KyThiId
                if (examPaper == null)
                {
                    if (string.IsNullOrEmpty(startDto.ExamPaperCode))
                    {
                        return new BaseResponseDto<ExamSubmissionDto> { Success = false, Message = "Kỳ thi chưa có đề thi" };
                    }
                    examPaper = await _examPaperRepository.GetByMaDeThiAsync(startDto.ExamPaperCode);
                }

                if (examPaper == null)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_NOT_FOUND",
                        $"User {taikhoanId} attempted to access non-existent exam {startDto.ExamPaperCode}",
                        taikhoanId, "Medium", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy đề thi"
                    };
                }

                // OWASP A01: Broken Access Control - Time-based access control
                if (examPaper.StartTime > DateTime.Now)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_EARLY_ACCESS_ATTEMPT",
                        $"User {taikhoanId} attempted early access to exam {examPaper.ExamPaperCode}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Chưa đến thời gian thi"
                    };
                }

                // Check for existing IN-PROGRESS session (resume it)
                var existingBaithi = await _examSubmissionRepository.GetActiveExamSessionAsync(taikhoanId, examPaper.Id);

                ExamSubmission examSubmission;
                if (existingBaithi != null && existingBaithi.Status == "InProgress")
                {
                    // Resume existing in-progress session
                    examSubmission = existingBaithi;
                    _logger.LogInformation("Resuming existing exam session {SessionId} for user {UserId}", examSubmission.Id, taikhoanId);
                }
                else
                {
                    // Create new session
                    var dethiWithQuestions = await _examPaperRepository.GetWithQuestionsAsync(examPaper.Id);

                    // Lấy toàn bộ câu hỏi, loại trùng lặp theo QuestionId
                    var allQuestions = (dethiWithQuestions?.ExamPaperQuestions ?? new List<ExamPaperQuestion>())
                        .Where(dc => dc.QuestionId.HasValue && dc.IdCauHoiNavigation != null)
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .Select(g => g.First())
                        .ToList();

                    // Đọc giới hạn số câu từ ChecksumData (nếu có)
                    int soCauLimit = allQuestions.Count;
                    if (!string.IsNullOrEmpty(examPaper.ChecksumData) && examPaper.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = examPaper.ChecksumData.Split('|');
                            var soCauPart = parts.FirstOrDefault(p => p.StartsWith("SO_CAU:"));
                            if (soCauPart != null && int.TryParse(soCauPart.Replace("SO_CAU:", ""), out int soCauConfig))
                                soCauLimit = Math.Min(soCauConfig, allQuestions.Count);
                        }
                        catch { /* ignore parse errors */ }
                    }

                    // Shuffle một lần duy nhất, dùng GUID để không thể đoán seed
                    var random = new Random(Guid.NewGuid().GetHashCode());
                    var selectedQuestions = allQuestions
                        .OrderBy(_ => random.Next())
                        .Take(soCauLimit)
                        .ToList();

                    examSubmission = new ExamSubmission
                    {
                        UserId = taikhoanId,
                        ExamPaperId = examPaper.Id,
                        ExamPaperCode = examPaper.ExamPaperCode,
                        ExamCampaignId = startDto.KyThiId ?? examPaper.KyThiId, // Link KyThiId to the exam session
                        Status = "InProgress",
                        TotalQuestions = selectedQuestions.Count,
                        TongSoCanhBao = 0,
                        StartTime = DateTime.Now
                    };

                    examSubmission = await _examSubmissionRepository.AddAsync(examSubmission);

                    // Lưu thứ tự câu hỏi đã chọn vào SubmissionDetail (placeholder, chưa có đáp án)
                    for (int i = 0; i < selectedQuestions.Count; i++)
                    {
                        var placeholder = new SubmissionDetail
                        {
                            ExamSubmissionId = examSubmission.Id,
                            QuestionId = selectedQuestions[i].QuestionId,
                            SelectedOptionId = null,
                            CauTraLoiTuLuan = null,
                            ThoiGianTraLoi = null,
                            DaLuu = false,
                            ScoreObtained = null
                        };
                        await _chitietRepository.AddAsync(placeholder);
                    }

                    await _securityService.LogSecurityEventAsync("EXAM_SESSION_STARTED",
                        $"User {taikhoanId} started exam session {examSubmission.Id} for exam {examPaper.ExamPaperCode} with {selectedQuestions.Count} questions",
                        taikhoanId, "Info", cancellationToken);

                    _logger.LogInformation("Created new exam session {SessionId} for user {UserId} with {Count} unique questions", examSubmission.Id, taikhoanId, selectedQuestions.Count);
                }

                var result = _mapper.Map<ExamSubmissionDto>(examSubmission);
                result.ExamPaperName = examPaper.ExamPaperName;
                result.DurationMinutes = examPaper.DurationMinutes;
                result.StartTime = examPaper.StartTime;

                // Calculate remaining time with security validation
                result.ThoiGianConLai = CalculateRemainingTime(examPaper, examSubmission.StartTime);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = true,
                    Message = "Bắt đầu làm bài thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting exam session for user {UserId} with exam code {ExamCode}", taikhoanId, startDto.ExamPaperCode);

                await _securityService.LogSecurityEventAsync("EXAM_START_ERROR",
                    $"System error during exam start for user {taikhoanId}: {ex.Message}",
                    taikhoanId, "High", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi bắt đầu thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting exam questions for session {SessionId} and user {UserId}", baithiId, taikhoanId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(baithiId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_EXAM_ACCESS",
                        $"User {taikhoanId} attempted unauthorized access to exam session {baithiId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<List<ExamQuestionDto>>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (examSubmission.Status != "InProgress")
                {
                    return new BaseResponseDto<List<ExamQuestionDto>>
                    {
                        Success = false,
                        Message = "Bài thi không ở trạng thái làm bài"
                    };
                }

                var examPaper = await _examPaperRepository.GetWithQuestionsAsync(examSubmission.ExamPaperId!.Value);
                if (examPaper == null)
                {
                    return new BaseResponseDto<List<ExamQuestionDto>>
                    {
                        Success = false,
                        Message = "Không tìm thấy đề thi"
                    };
                }

                var questions = new List<ExamQuestionDto>();

                // Đọc danh sách câu hỏi từ SubmissionDetail đã được lưu lúc StartExam
                // → thứ tự cố định, không shuffle lại, không bị trùng lặp
                var savedChitiets = await _chitietRepository.GetByBaiThiAsync(baithiId);

                // Distinct theo QuestionId để phòng trường hợp dữ liệu cũ có bản ghi trùng
                var orderedChitiets = savedChitiets
                    .Where(c => c.QuestionId.HasValue)
                    .GroupBy(c => c.QuestionId!.Value)
                    .Select(g => g.OrderBy(c => c.Id).First()) // lấy bản ghi cũ nhất
                    .OrderBy(c => c.Id)                         // giữ thứ tự insert lúc Start
                    .ToList();

                // Nếu session cũ (chưa có placeholder), fallback về logic cũ nhưng dùng seed cố định
                if (!orderedChitiets.Any())
                {
                    var random = new Random(baithiId);
                    int? soCauLimit = null;
                    if (!string.IsNullOrEmpty(examPaper.ChecksumData) && examPaper.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = examPaper.ChecksumData.Split('|');
                            var soCauPart = parts.FirstOrDefault(p => p.StartsWith("SO_CAU:"));
                            if (soCauPart != null && int.TryParse(soCauPart.Replace("SO_CAU:", ""), out int soCauConfig))
                                soCauLimit = soCauConfig;
                        }
                        catch { /* ignore */ }
                    }

                    var allDethiCauhois = examPaper.ExamPaperQuestions
                        .Where(dc => dc.QuestionId.HasValue && dc.IdCauHoiNavigation != null)
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .Select(g => g.First())
                        .ToList();

                    var shuffled = allDethiCauhois
                        .OrderBy(_ => random.Next())
                        .Take(soCauLimit.HasValue ? Math.Min(soCauLimit.Value, allDethiCauhois.Count) : allDethiCauhois.Count)
                        .ToList();

                    int thuTuFallback = 1;
                    foreach (var examPaperQuestion in shuffled)
                    {
                        var question = examPaperQuestion.IdCauHoiNavigation!;
                        var choiceRandom = new Random(baithiId * 1000 + question.Id);
                        var shuffledChoices = question.QuestionOptions
                            .OrderBy(_ => choiceRandom.Next())
                            .Select((l, idx) => new ExamChoiceDto { Id = l.Id, Content = SanitizeHtmlContent(l.Content), OrderIndex = idx + 1 })
                            .ToList();

                        questions.Add(new ExamQuestionDto
                        {
                            Id = question.Id,
                            Content = SanitizeHtmlContent(question.Content),
                            ImageUrl = question.ImageUrl,
                            ThuTuCau = thuTuFallback++,
                            Options = shuffledChoices,
                            ChoPhepChonNhieu = question.QuestionOptions.Count(l => l.IsCorrect == true) > 1
                        });
                    }
                }
                else
                {
                    // Nhóm chi tiết làm bài theo QuestionId để lấy toàn bộ đáp án đã chọn (hỗ trợ chọn nhiều)
                    var chitietsByQuestion = savedChitiets
                        .Where(c => c.QuestionId.HasValue)
                        .GroupBy(c => c.QuestionId!.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var cauhoiIds = orderedChitiets.Select(c => c.QuestionId!.Value).ToList();
                    var dethiCauhoisMap = examPaper.ExamPaperQuestions
                        .Where(dc => dc.QuestionId.HasValue && cauhoiIds.Contains(dc.QuestionId.Value))
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .ToDictionary(g => g.Key, g => g.First().IdCauHoiNavigation);

                    int orderIndex = 1;
                    foreach (var ct in orderedChitiets)
                    {
                        var cauHoiId = ct.QuestionId!.Value;
                        if (!dethiCauhoisMap.TryGetValue(cauHoiId, out var question) || question == null)
                            continue;

                        // Lấy tất cả lựa chọn đã chọn cho câu hỏi này
                        var questionChitiets = chitietsByQuestion.ContainsKey(cauHoiId) 
                            ? chitietsByQuestion[cauHoiId] 
                            : new List<SubmissionDetail> { ct };

                        var selectedChoiceIds = questionChitiets
                            .Where(c => c.SelectedOptionId.HasValue)
                            .Select(c => c.SelectedOptionId!.Value)
                            .ToList();

                        // Shuffle đáp án dùng seed cố định theo session + câu hỏi
                        var choiceRandom = new Random(baithiId * 1000 + question.Id);
                        var shuffledChoices = question.QuestionOptions
                            .OrderBy(_ => choiceRandom.Next())
                            .Select((l, idx) => new ExamChoiceDto
                            {
                                Id = l.Id,
                                Content = SanitizeHtmlContent(l.Content),
                                OrderIndex = idx + 1
                            })
                            .ToList();

                        var questionDto = new ExamQuestionDto
                        {
                            Id = question.Id,
                            Content = SanitizeHtmlContent(question.Content),
                            ImageUrl = question.ImageUrl,
                            ThuTuCau = orderIndex++,
                            Options = shuffledChoices,
                            SelectedOptionId = selectedChoiceIds.Count > 0 ? (int?)selectedChoiceIds[0] : null,
                            IdLuaChonDaChonList = selectedChoiceIds,
                            CauTraLoiTuLuan = SanitizeHtmlContent(questionChitiets.First().CauTraLoiTuLuan),
                            DaLuu = questionChitiets.Any(c => c.DaLuu ?? false),
                            ChoPhepChonNhieu = question.QuestionOptions.Count(l => l.IsCorrect == true) > 1
                        };

                        questions.Add(questionDto);
                    }
                }

                return new BaseResponseDto<List<ExamQuestionDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách câu hỏi thành công",
                    Data = questions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam questions for session {SessionId} and user {UserId}", baithiId, taikhoanId);

                await _securityService.LogSecurityEventAsync("EXAM_QUESTIONS_ERROR",
                    $"System error getting questions for user {taikhoanId}, session {baithiId}: {ex.Message}",
                    taikhoanId, "Medium", cancellationToken);

                return new BaseResponseDto<List<ExamQuestionDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting exam progress for session {SessionId} and user {UserId}", baithiId, taikhoanId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetWithDetailsAsync(baithiId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_PROGRESS_ACCESS",
                        $"User {taikhoanId} attempted unauthorized access to progress of session {baithiId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                var result = _mapper.Map<ExamSubmissionDto>(examSubmission);
                result.ExamPaperName = examSubmission.IdDeThiNavigation?.ExamPaperName;
                result.DurationMinutes = examSubmission.IdDeThiNavigation?.DurationMinutes;
                result.StartTime = examSubmission.IdDeThiNavigation?.StartTime;

                // Calculate remaining time
                result.ThoiGianConLai = CalculateRemainingTime(examSubmission.IdDeThiNavigation, examSubmission.StartTime);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = true,
                    Message = "Lấy tiến độ bài thi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam progress for session {SessionId} and user {UserId}", baithiId, taikhoanId);

                await _securityService.LogSecurityEventAsync("EXAM_PROGRESS_ERROR",
                    $"System error getting progress for user {taikhoanId}, session {baithiId}: {ex.Message}",
                    taikhoanId, "Medium", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy tiến độ bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> PauseExamAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Pausing exam session {SessionId} for user {UserId}", baithiId, taikhoanId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(baithiId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_PAUSE_ATTEMPT",
                        $"User {taikhoanId} attempted unauthorized pause of session {baithiId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không có quyền thao tác bài thi này"
                    };
                }

                if (examSubmission.Status != "InProgress")
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không thể tạm dừng bài thi ở trạng thái hiện tại"
                    };
                }

                examSubmission.Status = "Paused";
                await _examSubmissionRepository.UpdateAsync(examSubmission);

                await _securityService.LogSecurityEventAsync("EXAM_PAUSED",
                    $"User {taikhoanId} paused exam session {baithiId}",
                    taikhoanId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Tạm dừng bài thi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing exam session {SessionId} for user {UserId}", baithiId, taikhoanId);

                await _securityService.LogSecurityEventAsync("EXAM_PAUSE_ERROR",
                    $"System error pausing exam for user {taikhoanId}, session {baithiId}: {ex.Message}",
                    taikhoanId, "Medium", cancellationToken);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạm dừng bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> ResumeExamAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Resuming exam session {SessionId} for user {UserId}", baithiId, taikhoanId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(baithiId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_RESUME_ATTEMPT",
                        $"User {taikhoanId} attempted unauthorized resume of session {baithiId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không có quyền thao tác bài thi này"
                    };
                }

                if (examSubmission.Status != "Paused")
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Bài thi không ở trạng thái tạm dừng"
                    };
                }

                examSubmission.Status = "InProgress";
                await _examSubmissionRepository.UpdateAsync(examSubmission);

                await _securityService.LogSecurityEventAsync("EXAM_RESUMED",
                    $"User {taikhoanId} resumed exam session {baithiId}",
                    taikhoanId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Tiếp tục bài thi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming exam session {SessionId} for user {UserId}", baithiId, taikhoanId);

                await _securityService.LogSecurityEventAsync("EXAM_RESUME_ERROR",
                    $"System error resuming exam for user {taikhoanId}, session {baithiId}: {ex.Message}",
                    taikhoanId, "Medium", cancellationToken);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tiếp tục bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Calculate remaining exam time with security validation
        /// </summary>
        private int CalculateRemainingTime(ExamPaper? examPaper, DateTime? sessionStartTime)
        {
            if (examPaper?.StartTime == null || examPaper.DurationMinutes == null || sessionStartTime == null)
            {
                return 0;
            }

            var thoiGianDaLam = DateTime.Now - sessionStartTime.Value;
            var thoiGianConLai = (examPaper.DurationMinutes.Value * 60) - (int)thoiGianDaLam.TotalSeconds;
            return Math.Max(0, thoiGianConLai);
        }

        /// <summary>
        /// OWASP A03: Injection - Sanitize HTML content to prevent XSS
        /// </summary>
        private string? SanitizeHtmlContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Basic HTML sanitization - in production, use a proper HTML sanitizer like HtmlSanitizer
            return content
                .Replace("<script", "&lt;script")
                .Replace("</script>", "&lt;/script&gt;")
                .Replace("javascript:", "")
                .Replace("vbscript:", "")
                .Replace("onload=", "")
                .Replace("onerror=", "")
                .Replace("onclick=", "");
        }

        #endregion
    }
}