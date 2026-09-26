using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Hubs;
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
        private readonly ISubmissionDetailRepository _submissionDetailRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamSessionService> _logger;
        private readonly IExamMonitorNotifier _examMonitorNotifier; // [FIX] thiếu, cần để bắn NotifyExamStarted

        public ExamSessionService(
            IExamPaperRepository examPaperRepository,
            IExamSubmissionRepository baithiRepository,
            ISubmissionDetailRepository chitietRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamSessionService> logger,
            IExamMonitorNotifier examMonitorNotifier)
        {
            _examPaperRepository = examPaperRepository ?? throw new ArgumentNullException(nameof(examPaperRepository));
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _submissionDetailRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _examMonitorNotifier = examMonitorNotifier ?? throw new ArgumentNullException(nameof(examMonitorNotifier));
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting exam session for user {UserId} with exam code {ExamCode}, ExamCampaignId {ExamCampaignId}", userId, startDto.ExamPaperCode, startDto.ExamCampaignId);

                // OWASP A03: Injection - Input validation
                var validationResult = await _validationService.ValidateStartExamAsync(startDto, userId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_START_VALIDATION_FAILED",
                        $"User {userId} failed validation for exam {startDto.ExamPaperCode}",
                        userId, "Medium", cancellationToken);
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
                if (startDto.ExamCampaignId.HasValue)
                {
                    examPaper = await _examPaperRepository.ResolveExamForCandidateAsync(startDto.ExamCampaignId.Value, userId, cancellationToken);
                }

                // Fallback to ExamPaperCode if not resolved via ExamCampaignId
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
                        $"User {userId} attempted to access non-existent exam {startDto.ExamPaperCode}",
                        userId, "Medium", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy đề thi"
                    };
                }

                var vietnamTime = DateTime.UtcNow.AddHours(7);
                // OWASP A01: Broken Access Control - Time-based access control
                // BUG FIX: examPaper.StartTime is stored as TRUE UTC (copied from ExamCampaign.StartTime,
                // which the frontend sends via .toISOString()), while vietnamTime above is a FAKE VN time
                // (UtcNow + 7h) used elsewhere in this method as a display/storage convention.
                // Comparing a true-UTC value against a fake-VN value here made this gate off by ~7 hours.
                // Use a separate real-UTC now for this comparison only; vietnamTime itself is left
                // untouched because it is still correctly used below when stamping ExamSubmission.StartTime.
                var nowUtcForStartCheck = DateTime.UtcNow;

                // BUG FIX: ExamValidationService.ValidateStartExamAsync already bypasses this exact
                // gate for a practice-mode campaign (IsPracticeMode - exists forever, no real
                // schedule), but this method re-does its own independent StartTime check and had no
                // idea about that flag, so a practice exam paper with a future/placeholder StartTime
                // still got rejected here right after passing validation above. Same lookup, same bypass.
                var startCheckCampaignId = startDto.ExamCampaignId ?? examPaper.ExamCampaignId;
                var isPracticeMode = startCheckCampaignId.HasValue && await _context.ExamCampaigns
                    .AnyAsync(c => c.Id == startCheckCampaignId.Value && c.IsPracticeMode, cancellationToken);

                if (!isPracticeMode && examPaper.StartTime > nowUtcForStartCheck)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_EARLY_ACCESS_ATTEMPT",
                        $"User {userId} attempted early access to exam {examPaper.ExamPaperCode}",
                        userId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Chưa đến thời gian thi"
                    };
                }

                // Check for existing IN-PROGRESS session (resume it)
                var existingBaithi = await _examSubmissionRepository.GetActiveExamSessionAsync(userId, examPaper.Id);

                ExamSubmission? examSubmission = null;
                if (existingBaithi != null && existingBaithi.Status == "InProgress")
                {
                    if (existingBaithi.TotalQuestions == 0)
                    {
                        _logger.LogInformation("Deleting corrupted empty exam session {SessionId} for user {UserId}", existingBaithi.Id, userId);
                        await _examSubmissionRepository.DeleteAsync(existingBaithi.Id);
                    }
                    else
                    {
                        // Resume existing in-progress session
                        examSubmission = existingBaithi;
                        _logger.LogInformation("Resuming existing exam session {SessionId} for user {UserId}", examSubmission.Id, userId);
                    }
                }

                if (examSubmission == null)
                {
                    // Create new session
                    var examPaperWithQuestions = await _examPaperRepository.GetWithQuestionsAsync(examPaper.Id);

                    // Lấy toàn bộ câu hỏi, loại trùng lặp theo QuestionId
                    var allQuestions = (examPaperWithQuestions?.ExamPaperQuestions ?? new List<ExamPaperQuestion>())
                        .Where(dc => dc.QuestionId.HasValue && dc.Question != null)
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .Select(g => g.First())
                        .ToList();

                    if (allQuestions.Count == 0)
                    {
                        return new BaseResponseDto<ExamSubmissionDto>
                        {
                            Success = false,
                            Message = "Đề thi này chưa có câu hỏi nào. Vui lòng liên hệ quản trị viên."
                        };
                    }

                    // Đọc giới hạn số câu từ ChecksumData (nếu có)
                    int questionLimit = allQuestions.Count;
                    if (!string.IsNullOrEmpty(examPaper.ChecksumData) && examPaper.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = examPaper.ChecksumData.Split('|');
                            var soCauPart = parts.FirstOrDefault(p => p.StartsWith("SO_CAU:"));
                            if (soCauPart != null && int.TryParse(soCauPart.Replace("SO_CAU:", ""), out int soCauConfig))
                                questionLimit = Math.Min(soCauConfig, allQuestions.Count);
                        }
                        catch { /* ignore parse errors */ }
                    }

                    // Shuffle một lần duy nhất, dùng GUID để không thể đoán seed
                    var random = new Random(Guid.NewGuid().GetHashCode());
                    var selectedQuestions = allQuestions
                        .OrderBy(_ => random.Next())
                        .Take(questionLimit)
                        .ToList();

                    examSubmission = new ExamSubmission
                    {
                        UserId = userId,
                        ExamPaperId = examPaper.Id,
                        ExamPaperCode = examPaper.ExamPaperCode,
                        ExamCampaignId = startDto.ExamCampaignId ?? examPaper.ExamCampaignId, // Link ExamCampaignId to the exam session
                        Status = "InProgress",
                        TotalQuestions = selectedQuestions.Count,
                        WarningCount = 0,
                        StartTime = vietnamTime
                    };

                    // BUG FIX: without the DB-level unique index (UX_ExamSubmissions_User_Exam_InProgress,
                    // see Program.cs), a double-click / 2-tab retry on "start exam" could read "no active
                    // session" above twice and both branches would reach this insert, creating 2 InProgress
                    // rows with 2 different random question sets. The index now rejects the loser with a
                    // unique-constraint violation here - catch it and resume the winning row instead of
                    // surfacing a generic error to a user who just double-clicked a button.
                    ExamSubmission? raceWinner = null;
                    try
                    {
                        examSubmission = await _examSubmissionRepository.AddAsync(examSubmission);
                    }
                    catch (DbUpdateException)
                    {
                        raceWinner = await _examSubmissionRepository.GetActiveExamSessionAsync(userId, examPaper.Id);
                        if (raceWinner == null || raceWinner.Status != "InProgress")
                            throw;
                    }

                    if (raceWinner != null)
                    {
                        _logger.LogInformation("Lost the race to start exam session for user {UserId}, exam {ExamId} - resuming existing session {SessionId}", userId, examPaper.Id, raceWinner.Id);
                        examSubmission = raceWinner;
                        var result2 = _mapper.Map<ExamSubmissionDto>(examSubmission);
                        result2.ExamPaperName = examPaper.ExamPaperName;
                        result2.DurationMinutes = examPaper.DurationMinutes;
                        result2.StartTime = examPaper.StartTime;
                        result2.RemainingTimeSeconds = CalculateRemainingTime(examPaper, examSubmission.StartTime, await GetExtraMinutesAsync(userId, examPaper.Id));
                        return new BaseResponseDto<ExamSubmissionDto>
                        {
                            Success = true,
                            Message = "Bắt đầu làm bài thành công",
                            Data = result2
                        };
                    }

                    // Lưu thứ tự câu hỏi đã chọn vào SubmissionDetail (placeholder, chưa có đáp án)
                    for (int i = 0; i < selectedQuestions.Count; i++)
                    {
                        var placeholder = new SubmissionDetail
                        {
                            ExamSubmissionId = examSubmission.Id,
                            QuestionId = selectedQuestions[i].QuestionId,
                            SelectedOptionId = null,
                            EssayAnswer = null,
                            AnswerTime = null,
                            IsSaved = false,
                            ScoreObtained = null
                        };
                        await _submissionDetailRepository.AddAsync(placeholder);
                    }

                    await _securityService.LogSecurityEventAsync("EXAM_SESSION_STARTED",
                        $"User {userId} started exam session {examSubmission.Id} for exam {examPaper.ExamPaperCode} with {selectedQuestions.Count} questions",
                        userId, "Info", cancellationToken);

                    _logger.LogInformation("Created new exam session {SessionId} for user {UserId} with {Count} unique questions", examSubmission.Id, userId, selectedQuestions.Count);

                    // [FIX] Thiếu tín hiệu này -> màn hình Giám thị không biết có thí sinh mới vào thi,
                    // chỉ thấy khi F5 lại trang (mất ý nghĩa "thời gian thực").
                    if (examSubmission.ExamCampaignId.HasValue)
                    {
                        var startedUser = await _context.Users.FindAsync(new object?[] { userId }, cancellationToken);
                        await _examMonitorNotifier.NotifyExamStarted(
                            examSubmission.ExamCampaignId.Value,
                            userId,
                            startedUser?.FullName ?? "Unknown");
                    }
                }

                var result = _mapper.Map<ExamSubmissionDto>(examSubmission);
                result.ExamPaperName = examPaper.ExamPaperName;
                result.DurationMinutes = examPaper.DurationMinutes;
                result.StartTime = examPaper.StartTime;

                // Calculate remaining time with security validation
                result.RemainingTimeSeconds = CalculateRemainingTime(examPaper, examSubmission.StartTime, await GetExtraMinutesAsync(userId, examPaper.Id));

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = true,
                    Message = "Bắt đầu làm bài thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting exam session for user {UserId} with exam code {ExamCode}", userId, startDto.ExamPaperCode);

                await _securityService.LogSecurityEventAsync("EXAM_START_ERROR",
                    $"System error during exam start for user {userId}: {ex.Message}",
                    userId, "High", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi bắt đầu thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int examSubmissionId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting exam questions for session {SessionId} and user {UserId}", examSubmissionId, userId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_EXAM_ACCESS",
                        $"User {userId} attempted unauthorized access to exam session {examSubmissionId}",
                        userId, "High", cancellationToken);

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
                var savedChitiets = await _submissionDetailRepository.GetByBaiThiAsync(examSubmissionId);

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
                    var random = new Random(examSubmissionId);
                    int? questionLimit = null;
                    if (!string.IsNullOrEmpty(examPaper.ChecksumData) && examPaper.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = examPaper.ChecksumData.Split('|');
                            var soCauPart = parts.FirstOrDefault(p => p.StartsWith("SO_CAU:"));
                            if (soCauPart != null && int.TryParse(soCauPart.Replace("SO_CAU:", ""), out int soCauConfig))
                                questionLimit = soCauConfig;
                        }
                        catch { /* ignore */ }
                    }

                    var allExamPaperQuestions = examPaper.ExamPaperQuestions
                        .Where(dc => dc.QuestionId.HasValue && dc.Question != null)
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .Select(g => g.First())
                        .ToList();

                    var shuffled = allExamPaperQuestions
                        .OrderBy(_ => random.Next())
                        .Take(questionLimit.HasValue ? Math.Min(questionLimit.Value, allExamPaperQuestions.Count) : allExamPaperQuestions.Count)
                        .ToList();

                    int thuTuFallback = 1;
                    foreach (var examPaperQuestion in shuffled)
                    {
                        var question = examPaperQuestion.Question!;
                        var choiceRandom = new Random(examSubmissionId * 1000 + question.Id);
                        bool isEssay = question.QuestionOptions.Count < 2;
                        var shuffledChoices = isEssay ? new List<ExamChoiceDto>() : question.QuestionOptions
                            .OrderBy(_ => choiceRandom.Next())
                            .Select((l, idx) => new ExamChoiceDto { Id = l.Id, Content = SanitizeHtmlContent(l.Content), OrderIndex = idx + 1 })
                            .ToList();

                        questions.Add(new ExamQuestionDto
                        {
                            Id = question.Id,
                            Content = SanitizeHtmlContent(question.Content),
                            ImageUrl = question.ImageUrl,
                            QuestionOrder = thuTuFallback++,
                            Options = shuffledChoices,
                            AllowMultipleSelection = question.QuestionOptions.Count(l => l.IsCorrect == true) > 1
                        });
                    }
                }
                else
                {
                    // Nhóm chi tiết làm bài theo QuestionId để lấy toàn bộ đáp án đã chọn (hỗ trợ chọn nhiều)
                    var detailsByQuestion = savedChitiets
                        .Where(c => c.QuestionId.HasValue)
                        .GroupBy(c => c.QuestionId!.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var questionIds = orderedChitiets.Select(c => c.QuestionId!.Value).ToList();
                    var examPaperQuestionsMap = examPaper.ExamPaperQuestions
                        .Where(dc => dc.QuestionId.HasValue && questionIds.Contains(dc.QuestionId.Value))
                        .GroupBy(dc => dc.QuestionId!.Value)
                        .ToDictionary(g => g.Key, g => g.First().Question);

                    int orderIndex = 1;
                    foreach (var ct in orderedChitiets)
                    {
                        var qId = ct.QuestionId!.Value;
                        if (!examPaperQuestionsMap.TryGetValue(qId, out var question) || question == null)
                            continue;

                        // Lấy tất cả lựa chọn đã chọn cho câu hỏi này
                        var questionDetails = detailsByQuestion.ContainsKey(qId) 
                            ? detailsByQuestion[qId] 
                            : new List<SubmissionDetail> { ct };

                        var selectedChoiceIds = questionDetails
                            .Where(c => c.SelectedOptionId.HasValue)
                            .Select(c => c.SelectedOptionId!.Value)
                            .ToList();

                        // Shuffle đáp án dùng seed cố định theo session + câu hỏi
                        var choiceRandom = new Random(examSubmissionId * 1000 + question.Id);
                        bool isEssay = question.QuestionOptions.Count < 2;
                        var shuffledChoices = isEssay ? new List<ExamChoiceDto>() : question.QuestionOptions
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
                            QuestionOrder = orderIndex++,
                            Options = shuffledChoices,
                            SelectedOptionId = selectedChoiceIds.Count > 0 ? (int?)selectedChoiceIds[0] : null,
                            SelectedOptionIdList = selectedChoiceIds,
                            EssayAnswer = SanitizeHtmlContent(questionDetails.First().EssayAnswer),
                            EssayImageUrl = questionDetails.First().EssayImageUrl,
                            IsSaved = questionDetails.Any(c => c.IsSaved ?? false),
                            AllowMultipleSelection = question.QuestionOptions.Count(l => l.IsCorrect == true) > 1
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
                _logger.LogError(ex, "Error getting exam questions for session {SessionId} and user {UserId}", examSubmissionId, userId);

                await _securityService.LogSecurityEventAsync("EXAM_QUESTIONS_ERROR",
                    $"System error getting questions for user {userId}, session {examSubmissionId}: {ex.Message}",
                    userId, "Medium", cancellationToken);

                return new BaseResponseDto<List<ExamQuestionDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int examSubmissionId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting exam progress for session {SessionId} and user {UserId}", examSubmissionId, userId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetWithDetailsAsync(examSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_PROGRESS_ACCESS",
                        $"User {userId} attempted unauthorized access to progress of session {examSubmissionId}",
                        userId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                var result = _mapper.Map<ExamSubmissionDto>(examSubmission);
                result.ExamPaperName = examSubmission.ExamPaper?.ExamPaperName;
                result.DurationMinutes = examSubmission.ExamPaper?.DurationMinutes;
                result.StartTime = examSubmission.ExamPaper?.StartTime;

                // Calculate remaining time
                result.RemainingTimeSeconds = CalculateRemainingTime(examSubmission.ExamPaper, examSubmission.StartTime, await GetExtraMinutesAsync(userId, examSubmission.ExamPaperId));

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = true,
                    Message = "Lấy tiến độ bài thi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam progress for session {SessionId} and user {UserId}", examSubmissionId, userId);

                await _securityService.LogSecurityEventAsync("EXAM_PROGRESS_ERROR",
                    $"System error getting progress for user {userId}, session {examSubmissionId}: {ex.Message}",
                    userId, "Medium", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy tiến độ bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> PauseExamAsync(int examSubmissionId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Pausing exam session {SessionId} for user {UserId}", examSubmissionId, userId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_PAUSE_ATTEMPT",
                        $"User {userId} attempted unauthorized pause of session {examSubmissionId}",
                        userId, "High", cancellationToken);

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
                    $"User {userId} paused exam session {examSubmissionId}",
                    userId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Tạm dừng bài thi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing exam session {SessionId} for user {UserId}", examSubmissionId, userId);

                await _securityService.LogSecurityEventAsync("EXAM_PAUSE_ERROR",
                    $"System error pausing exam for user {userId}, session {examSubmissionId}: {ex.Message}",
                    userId, "Medium", cancellationToken);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạm dừng bài thi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> ResumeExamAsync(int examSubmissionId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Resuming exam session {SessionId} for user {UserId}", examSubmissionId, userId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_RESUME_ATTEMPT",
                        $"User {userId} attempted unauthorized resume of session {examSubmissionId}",
                        userId, "High", cancellationToken);

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
                    $"User {userId} resumed exam session {examSubmissionId}",
                    userId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Tiếp tục bài thi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming exam session {SessionId} for user {UserId}", examSubmissionId, userId);

                await _securityService.LogSecurityEventAsync("EXAM_RESUME_ERROR",
                    $"System error resuming exam for user {userId}, session {examSubmissionId}: {ex.Message}",
                    userId, "Medium", cancellationToken);

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
        private int CalculateRemainingTime(ExamPaper? examPaper, DateTime? sessionStartTime, int extraMinutes = 0)
        {
            if (examPaper?.DurationMinutes == null || sessionStartTime == null)
            {
                return 0;
            }

            int durationMinutes = examPaper.DurationMinutes.Value + extraMinutes;
            var endTime = sessionStartTime.Value.AddMinutes(durationMinutes);
            var vietnamTime = DateTime.UtcNow.AddHours(7);
            var remaining = (endTime - vietnamTime).TotalSeconds;
            return remaining > 0 ? (int)remaining : 0;
        }

        // BUG FIX: ExamAssignment.ExtraMinutes (granted via the supervisor "extend-time" action)
        // used to never be read anywhere - the countdown a student saw, and the auto-submit job's
        // expiry check, both silently ignored it, making "extend time" a no-op on the actual timer.
        private async Task<int> GetExtraMinutesAsync(int userId, int? examPaperId)
        {
            if (examPaperId == null) return 0;
            return await _context.ExamAssignments
                .Where(a => a.UserId == userId && a.ExamId == examPaperId.Value && a.IsActive)
                .Select(a => a.ExtraMinutes ?? 0)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// OWASP A03: Injection - Sanitize HTML content to prevent XSS
        /// BUG FIX: this used to be a case-sensitive .Replace() blacklist (e.g. "<script", "onerror=")
        /// which <SCRIPT, OnError=, JavaScript: etc. all bypassed completely. Replaced with the same
        /// case-insensitive regex-based approach already used in QuestionService.cs, for consistency.
        /// Still not a full HTML sanitizer (a real allowlist-based library like HtmlSanitizer/Ganss.Xss
        /// should replace this long-term), but it closes the trivial case-bypass.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex DangerousTagPattern = new(
            @"<\s*(script|iframe|object|embed|svg|link|meta|style)\b[^>]*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex DangerousAttrOrProtocolPattern = new(
            @"(on\w+\s*=)|(javascript\s*:)|(vbscript\s*:)|(data\s*:\s*text/html)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private string? SanitizeHtmlContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var sanitized = DangerousTagPattern.Replace(content, m => System.Net.WebUtility.HtmlEncode(m.Value));
            sanitized = DangerousAttrOrProtocolPattern.Replace(sanitized, "blocked:");
            return sanitized.Trim();
        }

        #endregion
    }
}
