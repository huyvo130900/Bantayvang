using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Helpers;
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
        private readonly ICheatWarningRepository _cheatWarningRepository;
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamService> _logger;
        private readonly Hubs.IExamMonitorNotifier _examMonitorNotifier;

        public ExamService(
            IExamManagementService managementService,
            IExamSessionService sessionService,
            IExamSubmissionService submissionService,
            IExamSecurityService securityService,
            ICheatWarningRepository cheatWarningRepository,
            IExamSubmissionRepository baithiRepository,
            BanTayVangDbContext context,
            ILogger<ExamService> logger,
            Hubs.IExamMonitorNotifier examMonitorNotifier)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _cheatWarningRepository = cheatWarningRepository ?? throw new ArgumentNullException(nameof(cheatWarningRepository));
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _examMonitorNotifier = examMonitorNotifier ?? throw new ArgumentNullException(nameof(examMonitorNotifier));
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

        public async Task<BaseResponseDto<ExamSubmissionDto>> StartExamAsync(StartExamDto startDto, int userId)
        {
            return await _sessionService.StartExamAsync(startDto, userId);
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int examSubmissionId, int userId)
        {
            return await _sessionService.GetExamQuestionsAsync(examSubmissionId, userId);
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> GetExamProgressAsync(int examSubmissionId, int userId)
        {
            return await _sessionService.GetExamProgressAsync(examSubmissionId, userId);
        }

        #endregion

        #region Exam Submission Operations (Delegated to IExamSubmissionService)

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int userId)
        {
            return await _submissionService.SaveAnswerAsync(answerDto, userId);
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int userId)
        {
            return await _submissionService.SubmitExamAsync(submitDto, userId);
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
        public async Task<BaseResponseDto<List<ExamSubmissionDto>>> GetMyResultsAsync(int userId)
        {
            try
            {
                var examSubmissions = await _examSubmissionRepository.GetByTaiKhoanAsync(userId);
                var completed = examSubmissions
                    .Where(b => b.Status == "Completed")
                    .OrderByDescending(b => b.SubmitTime ?? b.StartTime)
                    .GroupBy(b => b.ExamPaperCode ?? b.ExamPaperId?.ToString())
                    .Select(g => g.First())
                    .Select(b => {
                        var isPublished = b.IsIndividualResultPublished || (b.ExamPaper?.IsResultPublished ?? false);
                        return new ExamSubmissionDto
                        {
                            Id = b.Id,
                            UserId = b.UserId ?? 0,
                            ExamPaperId = b.ExamPaperId ?? 0,
                            Status = b.Status,
                            SubmitTime = b.SubmitTime,
                            TotalScore = isPublished ? b.TotalScore : null,
                            CorrectAnswers = isPublished ? b.CorrectAnswers : null,
                            TotalQuestions = b.TotalQuestions,
                            ExamPaperName = b.ExamPaper?.ExamPaperName,
                            ExamPaperCode = b.ExamPaperCode ?? b.ExamPaper?.ExamPaperCode,
                            StartTime = b.StartTime,
                            IsResultPublished = isPublished,
                            // BUG FIX: `?? 0` on a genuinely unconfigured MinPassQuestions collapsed
                            // into "CorrectAnswers >= 0", which is always true - a student with 0
                            // configured threshold saw Pass=true regardless of score, even 1/20.
                            // Uses the same shared rule as everywhere else (PassRuleHelper) instead
                            // of a locally copy-pasted version, to avoid the two silently drifting.
                            Pass = isPublished && PassRuleHelper.ComputePass(b.CorrectAnswers, b.ExamCampaign?.MinPassQuestions, b.ExamPaper?.MinPassQuestions),
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
                _logger.LogError(ex, "Error getting my results for user {UserId}", userId);
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

        public async Task<BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>>> GetActiveMonitorListAsync(int examCampaignId, int? myDeptId, bool isDeptManager = false)
        {
            try
            {
                // BUG FIX: `myDeptId` is null both for Admin (unrestricted, correct) AND for a
                // DeptManager whose managed_department_id claim is empty (created via the
                // single-user API path, which - unlike bulk import - doesn't require a department).
                // The old `if (myDeptId.HasValue)` skipped this whole check for the latter case too,
                // letting such an account see every department's live exam-monitoring data
                // (student names, employee codes, cheating-warning counts). The caller
                // (ExamController.GetActiveMonitorList) already resolves myDeptId via
                // DepartmentAuthHelper.GetDeptManagerDepartmentId, which itself returns null for
                // Admin - so we can't tell the two cases apart here. Callers must pass an
                // unambiguous flag instead of relying on null-means-admin.
                if (isDeptManager)
                {
                    if (myDeptId == null)
                        return new BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>> { Success = false, Message = "Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý." };

                    var campaignExists = await _context.ExamCampaigns.AnyAsync(k => k.Id == examCampaignId);
                    if (!campaignExists)
                        return new BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>> { Success = false, Message = "Không tìm thấy kỳ thi" };

                    // A campaign can now be scoped to 1-n departments (ExamCampaignDepartments)
                    // instead of one - check membership there instead of the old DepartmentId column.
                    var isLinkedToMyDept = await _context.Set<ExamCampaignDepartment>()
                        .AnyAsync(kd => kd.ExamCampaignId == examCampaignId && kd.DepartmentId == myDeptId.Value);
                    if (!isLinkedToMyDept)
                        return new BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>> { Success = false, Message = "Kỳ thi này không thuộc khoa bạn quản lý" };
                }

                var sessions = await _examSubmissionRepository.GetActiveSessionsByCampaignAsync(examCampaignId);
                
                var dtos = sessions.Select(s => new DTOs.AntiCheat.ActiveStudentMonitorDto
                {
                    ExamSubmissionId = s.Id,
                    UserId = s.UserId ?? 0,
                    FullName = s.User?.FullName ?? "Unknown",
                    UserCode = s.User?.EmployeeCode,
                    ExamPaperCode = s.ExamPaper?.ExamPaperCode,
                    WarningCount = s.WarningCount ?? 0,
                    StartTime = s.StartTime,
                    Status = s.Status
                }).ToList();

                return new BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>>
                {
                    Success = true,
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active monitor list for campaign {CampaignId}", examCampaignId);
                return new BaseResponseDto<List<DTOs.AntiCheat.ActiveStudentMonitorDto>> { Success = false, Message = "Lỗi khi lấy danh sách giám sát" };
            }
        }

        public async Task<BaseResponseDto> ForceSubmitAsync(int examSubmissionId, int? supervisorId, int? supervisorDeptId, string? supervisorDeptName = null, bool isDeptManager = false)
        {
            return await _submissionService.ForceSubmitAsync(examSubmissionId, supervisorId, supervisorDeptId, supervisorDeptName, isDeptManager);
        }

        public async Task<BaseResponseDto> LogSuspiciousActivityAsync(int examSubmissionId, int userId, string warningType, string description)
        {
            try
            {
                _logger.LogWarning("Suspicious activity detected - Session: {SessionId}, Type: {Type}, Description: {Description}",
                    examSubmissionId, warningType, description);

                var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
                if (examSubmission != null)
                {
                    if (examSubmission.UserId != userId)
                    {
                        return new BaseResponseDto { Success = false, Message = "Người dùng không hợp lệ" };
                    }
                    if (examSubmission.Status == "Completed")
                    {
                        return new BaseResponseDto
                        {
                            Success = false,
                            Message = "Bài thi đã kết thúc, không thể ghi nhận cảnh báo"
                        };
                    }

                    // [FIX] Race condition: đọc WarningCount, +1 rồi UpdateAsync (read-modify-write) có thể
                    // mất dữ liệu nếu thí sinh đổi tab liên tục -> nhiều request chạm cùng lúc, request sau
                    // ghi đè request trước dựa trên giá trị cũ. Chuyển sang ExecuteUpdateAsync để DB tự cộng
                    // dồn nguyên tử (tương đương UPDATE ... SET WarningCount = WarningCount + 1), không cần
                    // đọc giá trị cũ vào bộ nhớ trước.
                    if (warningType == "FULLSCREEN_EXIT")
                    {
                        await _context.ExamSubmissions
                            .Where(e => e.Id == examSubmissionId)
                            .ExecuteUpdateAsync(s => s.SetProperty(
                                e => e.WarningCount,
                                e => (e.WarningCount ?? 0) < 6 ? 6 : e.WarningCount));
                    }
                    else
                    {
                        await _context.ExamSubmissions
                            .Where(e => e.Id == examSubmissionId)
                            .ExecuteUpdateAsync(s => s.SetProperty(
                                e => e.WarningCount,
                                e => (e.WarningCount ?? 0) + 1));
                    }

                    // Đọc lại giá trị thật vừa ghi (không dùng examSubmission.WarningCount trong bộ nhớ nữa
                    // vì có thể đã lỗi thời so với DB do request khác chạy song song).
                    examSubmission.WarningCount = await _context.ExamSubmissions
                        .Where(e => e.Id == examSubmissionId)
                        .Select(e => e.WarningCount)
                        .FirstOrDefaultAsync();
                }

                // OWASP A09: Security Logging - Enhanced security event logging
                var warning = new CheatWarning
                {
                    ExamSubmissionId = examSubmissionId,
                    WarningType = warningType,
                    Description = SanitizeInput(description), // OWASP A03: Injection prevention
                    ActionTime = DateTime.UtcNow.AddHours(7)
                };

                await _cheatWarningRepository.AddAsync(warning);

                // Log to security service for centralized monitoring
                await _securityService.LogSecurityEventAsync(
                    $"SUSPICIOUS_ACTIVITY_{warningType}",
                    description,
                    null, // Will be extracted from session
                    DetermineSeverityLevel(warningType));

                if (examSubmission != null && examSubmission.ExamCampaignId.HasValue)
                {
                    var user = examSubmission.User ?? await _context.Users.FindAsync(examSubmission.UserId);
                    await _examMonitorNotifier.NotifyCheatingWarning(
                        examSubmission.ExamCampaignId.Value,
                        examSubmissionId,
                        user?.FullName ?? "Unknown",
                        warningType,
                        description,
                        examSubmission.WarningCount ?? 0);
                }

                // [FIX v2] Trước đây việc "ép nộp bài" hoàn toàn phó mặc cho Frontend gọi onForceSubmit.
                // Thí sinh có thể chặn/sửa JS để hàm đó không bao giờ chạy -> backend vẫn ghi nhận đủ
                // số lần vi phạm nhưng bài thi vẫn ở trạng thái InProgress vô thời hạn. Chốt quyền
                // sinh sát ở Server: khi đạt ngưỡng, tự gọi ForceSubmitAsync NGAY SAU KHI đã ghi nhận
                // và thông báo cảnh báo thứ N (không return sớm, để CheatWarning + NotifyCheatingWarning
                // của chính lần vi phạm gây khóa vẫn được lưu/hiển thị trên activity feed của giám thị).
                // BUG FIX: FE only terminates/calls onForceSubmit when warningCount > MAX_CHEATING_WARNINGS
                // (i.e. on the 7th violation - see isTerminated in use-anti-cheat.ts), but this used
                // `>= maxCheatingWarnings` which force-submitted on the 6th violation already - one
                // violation earlier than the student's own UI believes the exam ends. That silently
                // locked the submission server-side (Status becomes "Completed") while the student's
                // screen still showed the exam as in-progress, so their next answer/warning calls would
                // start failing with "Bài thi đã kết thúc" for no reason they could see on screen.
                const int maxCheatingWarnings = 6; // đồng bộ với MAX_CHEATING_WARNINGS bên FE (lib/constants.ts)
                if (examSubmission != null &&
                    (warningType == "FULLSCREEN_EXIT" || (examSubmission.WarningCount ?? 0) > maxCheatingWarnings))
                {
                    await _submissionService.ForceSubmitAsync(examSubmissionId, null, null);

                    return new BaseResponseDto
                    {
                        Success = true,
                        Message = "Đã ghi nhận cảnh báo. Bài thi đã bị hệ thống tự động khóa do vượt ngưỡng vi phạm."
                    };
                }

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đã ghi nhận cảnh báo bảo mật"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging suspicious activity for session {SessionId}", examSubmissionId);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi ghi nhận cảnh báo",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<int>> GetWarningCountAsync(int examSubmissionId)
        {
            try
            {
                var count = await _cheatWarningRepository.GetTotalWarningsAsync(examSubmissionId);

                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = "Lấy số lượng cảnh báo thành công",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warning count for session {SessionId}", examSubmissionId);

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
        private string DetermineSeverityLevel(string warningType)
        {
            return warningType.ToUpperInvariant() switch
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
                    .AsNoTracking()
                    .Include(d => d.ExamCampaign)
                    .Include(d => d.ExamPaperQuestions)
                        .ThenInclude(dc => dc.Question)
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
                            Id = dc.Question?.Id ?? 0,
                            Content = dc.Question?.Content,
                            ImageUrl = dc.Question?.ImageUrl,
                            ChuDe = null,
                            // Shuffle thứ tự đáp án
                            QuestionOptions = dc.Question?.QuestionOptions
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
                        .AsNoTracking()
                        .Include(c => c.QuestionOptions)
                        .Where(c => c.Department == examPaper.Department && c.IsDeleted != true)
                        .ToListAsync();

                    // Shuffle ngẫu nhiên mỗi lần preview (dùng lại rng ở trên), lọc trùng lặp theo nội dung câu hỏi
                    var poolQuestions = allPoolQuestions
                        .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                        .Select(g => g.First())
                        .OrderBy(_ => rng.Next())
                        .Take(examPaper.ExamCampaign?.TotalQuestions ?? 10)
                        .ToList();

                    preview.Questions = poolQuestions.Select(c => new QuestionPreviewDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        ImageUrl = c.ImageUrl,
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

