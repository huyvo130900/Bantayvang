using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Validation;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BanTayVang.API.Services.Impl.Validation
{
    /// <summary>
    /// Exam validation service implementing OWASP security standards
    /// Follows SRP - focused on validation only
    /// </summary>
    public class ExamValidationService : IExamValidationService
    {
        private readonly IExamPaperRepository _examPaperRepository;
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamValidationService> _logger;

        // OWASP: Define security patterns
        private static readonly Regex SafeTextPattern = new(@"^[^<>""'%;()&+]*$", RegexOptions.Compiled);
        private static readonly Regex ExamCodePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        // BUG FIX: the old pattern blocked any input containing a bare SQL keyword
        // (SELECT, INSERT, UPDATE, DELETE, ...) as a whole word, which has no real security
        // value here - all queries in this app go through EF Core LINQ (parameterized), never
        // raw string-concatenated SQL - but it does false-positive on legitimate exam names and
        // essay answers that use those words as ordinary English/medical terms (e.g. "Insert
        // catheter tinh mach", "Select benh nhan phu hop"). Require actual SQL syntax around the
        // keyword (comment markers, statement terminators, tautologies, UNION SELECT, or a
        // keyword followed by its normal SQL clause) so plain natural-language use is not flagged.
        private static readonly Regex SqlInjectionPattern = new(
            @"(--|;\s*--|/\*.*?\*/)" +
            @"|('\s*(or|and)\s+.+?=)" +
            @"|(\bunion\b(\s+all)?\s+\bselect\b)" +
            @"|(\b(select\s+.+?\s+from|insert\s+into|delete\s+from|drop\s+(table|database)|update\s+\S+\s+set|alter\s+(table|database)|exec(ute)?\s*\(|xp_cmdshell|sp_executesql)\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ExamValidationService(
            IExamPaperRepository examPaperRepository,
            IExamSubmissionRepository baithiRepository,
            IQuestionRepository questionRepository,
            BanTayVangDbContext context,
            ILogger<ExamValidationService> logger)
        {
            _examPaperRepository = examPaperRepository;
            _examSubmissionRepository = baithiRepository;
            _questionRepository = questionRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResultDto> ValidateCreateExamAsync(CreateExamPaperDto createDto, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // OWASP A03: Injection Prevention
                if (ContainsSqlInjection(createDto.ExamPaperCode) || ContainsSqlInjection(createDto.ExamPaperName))
                {
                    errors.Add("Input contains potentially malicious content");
                    _logger.LogWarning("SQL injection attempt detected in exam creation: {ExamCode}", createDto.ExamPaperCode);
                    return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                }

                // OWASP A03: XSS Prevention
                if (!string.IsNullOrEmpty(createDto.ExamPaperName) && !SafeTextPattern.IsMatch(createDto.ExamPaperName))
                {
                    errors.Add("Exam name contains invalid characters that could pose security risks");
                }

                if (!ExamCodePattern.IsMatch(createDto.ExamPaperCode))
                {
                    errors.Add("Exam code format is invalid");
                }

                // Business validation
                var existingExam = await _examPaperRepository.GetByMaDeThiAsync(createDto.ExamPaperCode);
                if (existingExam != null)
                {
                    errors.Add("Exam code already exists");
                }

                // OWASP A04: Insecure Design - Validate reasonable limits
                if (createDto.DurationMinutes.HasValue && createDto.DurationMinutes.Value > 1008000)
                {
                    errors.Add("Exam duration exceeds maximum allowed time");
                }

                if (createDto.QuestionIds.Count > 200)
                {
                    errors.Add("Too many questions (maximum 200 allowed)");
                }

                // Validate question IDs exist and are accessible
                if (createDto.QuestionIds.Any())
                {
                    var validQuestionIds = await _questionRepository.GetValidQuestionIdsAsync(createDto.QuestionIds);
                    var invalidIds = createDto.QuestionIds.Except(validQuestionIds).ToList();

                    if (invalidIds.Any())
                    {
                        errors.Add($"Invalid question IDs: {string.Join(", ", invalidIds)}");
                    }
                }

                // BUG FIX: createDto.StartTime becomes ExamPaper.StartTime, which is stored as
                // TRUE UTC (frontend sends via .toISOString()) - same class of bug already fixed
                // a few lines below in ValidateStartExamAsync (see "round 2" comment) and in
                // UpdateExamPaperDto's equivalent check, just missed here. Comparing it against
                // the fake-VN-time UtcNow.AddHours(7) meant any start time within the next ~7
                // hours (a perfectly valid near-future time) incorrectly tripped this warning.
                if (createDto.StartTime < DateTime.UtcNow.AddMinutes(-10))
                {
                    warnings.Add("Start time is in the past");
                }

                if (errors.Any())
                {
                    return ValidationResultDto.Failure(errors, "VALIDATION_FAILED");
                }

                // BUG FIX (mục 43): ValidationResultDto.Success() luôn trả về Warnings rỗng,
                // nên cảnh báo ở trên ("Start time is in the past") trước đây không bao giờ thực sự đến được response.
                var successResult = ValidationResultDto.Success();
                successResult.Warnings = warnings;
                return successResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam creation");
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateStartExamAsync(StartExamDto startDto, int userId, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // OWASP A01: Broken Access Control - Validate user permissions
                if (userId <= 0)
                {
                    errors.Add("Invalid user ID");
                    return ValidationResultDto.Failure(errors, "INVALID_USER");
                }

                // Resolve exam paper (ExamPaper)
                ExamPaper? exam = null;
                if (startDto.ExamCampaignId.HasValue)
                {
                    exam = await _examPaperRepository.ResolveExamForCandidateAsync(startDto.ExamCampaignId.Value, userId, cancellationToken);
                }
                else
                {
                    // OWASP A03: Injection Prevention
                    if (ContainsSqlInjection(startDto.ExamPaperCode))
                    {
                        errors.Add("Exam code contains invalid characters");
                        _logger.LogWarning("SQL injection attempt in exam start: {ExamCode} by user {UserId}", startDto.ExamPaperCode, userId);
                        return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                    }

                    // Validate exam exists
                    exam = await _examPaperRepository.GetByMaDeThiAsync(startDto.ExamPaperCode);
                }

                if (exam == null)
                {
                    errors.Add("Không tìm thấy đề thi");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_FOUND");
                }

                // FIX 1: Bỏ check cứng Status == "Active"
                // Chỉ block nếu đề bị đóng/hủy rõ ràng
                var closedStatuses = new[] { "Closed", "DaDong", "Inactive", "Cancelled" };
                if (closedStatuses.Contains(exam.Status, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add("Đề thi đã đóng, không thể thi");
                    return ValidationResultDto.Failure(errors, "EXAM_CLOSED");
                }

                // FIX 2: Dùng DateTime.UtcNow.AddHours(7) (giờ VN) thay vì DateTime.UtcNow.AddHours(7) phụ thuộc vào timezone của server
                // BUG FIX (round 2): exam.StartTime (ExamPaper.StartTime) is stored as TRUE UTC
                // (copied from ExamCampaign.StartTime, set via the frontend's .toISOString()), NOT the
                // fake-VN convention used elsewhere in this codebase. Comparing it against
                // DateTime.UtcNow.AddHours(7) made this gate off by ~7 hours. Compare against real UTC instead.
                var nowUtcForStartCheck = DateTime.UtcNow;

                // Kỳ thi luyện tập (IsPracticeMode) tồn tại vĩnh viễn - cần biết cờ này TRƯỚC khi
                // kiểm tra StartTime, để bỏ qua luôn cả gate "chưa đến giờ thi" bên dưới.
                var campaignIdForStatusCheck = startDto.ExamCampaignId ?? exam.ExamCampaignId;
                var campaignInfo = campaignIdForStatusCheck.HasValue
                    ? await _context.ExamCampaigns
                        .Where(c => c.Id == campaignIdForStatusCheck.Value)
                        .Select(c => new { c.Status, c.AccessMode, c.IsPracticeMode })
                        .FirstOrDefaultAsync(cancellationToken)
                    : null;
                var isPracticeMode = campaignInfo?.IsPracticeMode == true;

                if (!isPracticeMode && exam.StartTime.HasValue && exam.StartTime > nowUtcForStartCheck)
                {
                    errors.Add("Chưa đến thời gian thi");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_STARTED");
                }

                // BUG FIX: ExamCampaign.Status ("TamDung"/"DaKetThuc", set via the admin/DeptManager
                // "change status" dropdown) used to never be checked here at all - pausing or manually
                // ending a campaign only changed the badge shown in the admin UI, with zero effect on
                // whether a student could still start a brand-new attempt while the campaign's own
                // StartTime/EndTime window was still open. This is the actual emergency-stop gate.
                if (campaignIdForStatusCheck.HasValue)
                {
                    if (campaignInfo?.Status == "TamDung")
                    {
                        errors.Add("Kỳ thi đang tạm dừng, vui lòng thử lại sau");
                        return ValidationResultDto.Failure(errors, "EXAM_CAMPAIGN_PAUSED");
                    }
                    if (campaignInfo?.Status == "DaKetThuc")
                    {
                        errors.Add("Kỳ thi đã kết thúc");
                        return ValidationResultDto.Failure(errors, "EXAM_CAMPAIGN_ENDED");
                    }

                    // BUG FIX (feature): a campaign in "AssignedList" mode is only supposed to let
                    // in the specific people an admin uploaded (see ExamCampaignController's
                    // assign-from-excel) - before this check, ExamAssignment rows were written but
                    // never read anywhere in the actual start-exam path, so ANY authenticated user
                    // could still start it same as a normal campaign. This is the real gate; the
                    // department-based path (AccessMode == "Department") is unaffected and keeps
                    // relying on ExamCampaignService.GetAllAsync's list-visibility filter as before.
                    if (campaignInfo?.AccessMode == "AssignedList")
                    {
                        var isAssigned = await _context.ExamAssignments
                            .AnyAsync(a => a.UserId == userId && a.ExamId == exam.Id && a.IsActive, cancellationToken);
                        if (!isAssigned)
                        {
                            errors.Add("Bạn không có trong danh sách được chỉ định thi kỳ thi này");
                            return ValidationResultDto.Failure(errors, "NOT_ASSIGNED");
                        }
                    }
                }

                // Check if user already completed this exam / ExamCampaign
                // Allow retakes if still within duration of ExamCampaign (or ExamPaper if standalone)
                var studentSessions = await _examSubmissionRepository.GetByTaiKhoanAsync(userId);
                bool withinDuration = false;

                if (startDto.ExamCampaignId.HasValue)
                {
                    var examCampaign = await _context.ExamCampaigns.FindAsync(startDto.ExamCampaignId.Value, cancellationToken);
                    if (examCampaign != null)
                    {
                        var start = examCampaign.StartTime;
                        var end = examCampaign.EndTime;
                        if ((!start.HasValue || nowUtcForStartCheck >= start.Value) && (!end.HasValue || nowUtcForStartCheck <= end.Value))
                        {
                            withinDuration = true;
                        }
                    }
                }
                else
                {
                    var start = exam.StartTime;
                    var duration = exam.DurationMinutes ?? 60;
                    if (start.HasValue && nowUtcForStartCheck >= start.Value && nowUtcForStartCheck <= start.Value.AddMinutes(duration))
                    {
                        withinDuration = true;
                    }
                }

                // Kỳ thi luyện tập cho phép làm lại không giới hạn số lần, bất kể thời gian - bỏ
                // qua hẳn khối kiểm tra "đã hết hạn/đã nộp bài, không được thi lại" bên dưới.
                if (!withinDuration && !isPracticeMode)
                {
                    if (startDto.ExamCampaignId.HasValue)
                    {
                        var completedSessionInCampaign = studentSessions.Any(b => b.ExamCampaignId == startDto.ExamCampaignId.Value
                            && b.Status != "InProgress" && b.Status != "Paused");
                        if (completedSessionInCampaign)
                        {
                            errors.Add("Kỳ thi đã hết hạn hoặc bạn đã nộp bài thi cho kỳ thi này và không được phép thi lại.");
                            return ValidationResultDto.Failure(errors, "EXAM_ALREADY_TAKEN");
                        }
                    }

                    var completedSessionForExamPaper = studentSessions.Any(b => b.ExamPaperId == exam.Id
                        && b.Status != "InProgress" && b.Status != "Paused");
                    if (completedSessionForExamPaper)
                    {
                        errors.Add("Đề thi đã hết hạn hoặc bạn đã nộp đề thi này và không được phép thi lại.");
                        return ValidationResultDto.Failure(errors, "EXAM_ALREADY_TAKEN");
                    }
                }

                // Check if user has an active session for this exam (concurrency check)
                var existingSession = await _examSubmissionRepository.GetActiveExamSessionAsync(userId, exam.Id);

                // OWASP A04: Validate concurrent session limits
                var activeSessions = await _examSubmissionRepository.GetActiveSessionsCountAsync(userId);
                if (activeSessions >= 3) // Max 3 concurrent exams
                {
                    errors.Add("Too many active exam sessions");
                    return ValidationResultDto.Failure(errors, "TOO_MANY_SESSIONS");
                }

                return ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam start for user {UserId}", userId);
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateAnswerSubmissionAsync(SubmitAnswerDto answerDto, int userId, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // OWASP A01: Broken Access Control
                var examSession = await _examSubmissionRepository.GetByIdAsync(answerDto.ExamSubmissionId);
                if (examSession == null)
                {
                    errors.Add("Exam session not found");
                    return ValidationResultDto.Failure(errors, "SESSION_NOT_FOUND");
                }

                if (examSession.UserId != userId)
                {
                    errors.Add("Access denied to this exam session");
                    _logger.LogWarning("Unauthorized access attempt to exam session {SessionId} by user {UserId}", answerDto.ExamSubmissionId, userId);
                    return ValidationResultDto.Failure(errors, "ACCESS_DENIED");
                }

                if (examSession.Status != "InProgress")
                {
                    errors.Add("Exam session is not active");
                    return ValidationResultDto.Failure(errors, "SESSION_INACTIVE");
                }

                // BUG FIX: same gap as ValidateStartExamAsync - a student already mid-exam kept
                // being able to save answers and submit even after an admin/DeptManager paused or
                // manually ended the campaign, because nothing here ever checked ExamCampaign.Status.
                // This call is shared by both SaveAnswerAsync (per-answer) and, via
                // ValidateExamSubmissionAsync, the final SubmitExamAsync - so this one check covers
                // both "save answer" and "submit exam" for an in-progress session.
                if (examSession.ExamCampaignId.HasValue)
                {
                    var campaignStatus = await _context.ExamCampaigns
                        .Where(c => c.Id == examSession.ExamCampaignId.Value)
                        .Select(c => c.Status)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (campaignStatus == "TamDung" || campaignStatus == "DaKetThuc")
                    {
                        errors.Add("Kỳ thi đã bị tạm dừng hoặc kết thúc, không thể tiếp tục làm bài");
                        return ValidationResultDto.Failure(errors, "EXAM_CAMPAIGN_PAUSED");
                    }
                }

                // OWASP A03: Injection Prevention for essay answers
                if (!string.IsNullOrEmpty(answerDto.EssayAnswer))
                {
                    if (ContainsSqlInjection(answerDto.EssayAnswer))
                    {
                        errors.Add("Answer contains invalid content");
                        _logger.LogWarning("SQL injection attempt in answer submission by user {UserId}", userId);
                        return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                    }

                    // OWASP A04: Validate answer length
                    if (answerDto.EssayAnswer.Length > 5000)
                    {
                        errors.Add("Answer is too long (maximum 5000 characters)");
                    }
                }

                // Validate question belongs to this exam
                var examWithQuestions = await _examPaperRepository.GetWithQuestionsAsync(examSession.ExamPaperId!.Value);
                var questionExists = examWithQuestions?.ExamPaperQuestions.Any(dc => dc.QuestionId == answerDto.QuestionId) ?? false;

                if (!questionExists)
                {
                    errors.Add("Question does not belong to this exam");
                    _logger.LogWarning("Invalid question access attempt: Question {QuestionId} in session {SessionId} by user {UserId}",
                        answerDto.QuestionId, answerDto.ExamSubmissionId, userId);
                    return ValidationResultDto.Failure(errors, "INVALID_QUESTION");
                }

                return ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating answer submission for user {UserId}", userId);
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateExamPermissionAsync(int examId, int userId, string operation, CancellationToken cancellationToken = default)
        {
            try
            {
                // OWASP A01: Broken Access Control - Implement proper authorization
                if (userId <= 0)
                {
                    return ValidationResultDto.Failure("Invalid user", "INVALID_USER");
                }

                // For CREATE operations, no examId check needed
                if (operation.ToUpper() == "CREATE")
                {
                    return ValidationResultDto.Success();
                }

                var exam = await _examPaperRepository.GetByIdAsync(examId);
                if (exam == null)
                {
                    return ValidationResultDto.Failure("Exam not found", "EXAM_NOT_FOUND");
                }

                var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (user == null)
                {
                    return ValidationResultDto.Failure("User not found", "USER_NOT_FOUND");
                }

                // Basic permission check (should be enhanced with role-based access)
                switch (operation.ToUpper())
                {
                    case "UPDATE":
                    case "DELETE":
                        // Admin can modify any exam
                        if (user.RoleId == 1)
                        {
                            break;
                        }

                        // DeptManager (RoleId == 5) can modify exams of their department
                        if (user.RoleId == 5 && !string.IsNullOrEmpty(user.Department) && exam.Department == user.Department)
                        {
                            break;
                        }

                        // Creator can modify their own exam
                        if (exam.CreatedBy == userId)
                        {
                            break;
                        }

                        _logger.LogWarning("Unauthorized {Operation} attempt on exam {ExamId} by user {UserId}", operation, examId, userId);
                        return ValidationResultDto.Failure("Access denied", "ACCESS_DENIED");

                    case "VIEW":
                    case "TAKE":
                        // All authenticated users can view/take active exams
                        if (exam.Status != "Active" && operation == "TAKE")
                        {
                            return ValidationResultDto.Failure("Exam is not available for taking", "EXAM_INACTIVE");
                        }
                        break;

                    default:
                        return ValidationResultDto.Failure("Invalid operation", "INVALID_OPERATION");
                }

                return ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam permission for user {UserId}", userId);
                return ValidationResultDto.Failure("Permission validation error", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateUpdateExamAsync(UpdateExamPaperDto updateDto, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // OWASP A03: Injection Prevention
                if (ContainsSqlInjection(updateDto.ExamPaperCode) || ContainsSqlInjection(updateDto.ExamPaperName))
                {
                    errors.Add("Input contains potentially malicious content");
                    return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                }

                // Validate exam exists
                var existingExam = await _examPaperRepository.GetByIdAsync(updateDto.Id);
                if (existingExam == null)
                {
                    errors.Add("Exam not found");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_FOUND");
                }

                // Check if exam code is being changed and if new code exists
                if (existingExam.ExamPaperCode != updateDto.ExamPaperCode)
                {
                    var duplicateExam = await _examPaperRepository.GetByMaDeThiAsync(updateDto.ExamPaperCode);
                    if (duplicateExam != null)
                    {
                        errors.Add("Exam code already exists");
                    }
                }

                // Validate business rules
                if (existingExam.Status == "Active" && updateDto.Status == "Inactive")
                {
                    // Check if there are active sessions
                    var activeSessions = await _examSubmissionRepository.GetActiveSessionsByExamAsync(updateDto.Id);
                    if (activeSessions.Any())
                    {
                        errors.Add("Cannot deactivate exam with active sessions");
                    }
                }

                return errors.Any()
                    ? ValidationResultDto.Failure(errors, "VALIDATION_FAILED")
                    : ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam update");
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateExamSubmissionAsync(SubmitExamDto submitDto, int userId, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // Validate session access
                var sessionValidation = await ValidateAnswerSubmissionAsync(
                    new SubmitAnswerDto { ExamSubmissionId = submitDto.ExamSubmissionId },
                    userId,
                    cancellationToken);

                if (!sessionValidation.IsValid)
                {
                    return sessionValidation;
                }

                // Validate all answers in submission
                foreach (var answer in submitDto.Answers)
                {
                    var answerValidation = await ValidateAnswerSubmissionAsync(answer, userId, cancellationToken);
                    if (!answerValidation.IsValid)
                    {
                        errors.AddRange(answerValidation.Errors);
                    }
                }

                return errors.Any()
                    ? ValidationResultDto.Failure(errors, "SUBMISSION_VALIDATION_FAILED")
                    : ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam submission for user {UserId}", userId);
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        /// <summary>
        /// OWASP A03: Injection Prevention - Check for SQL injection patterns
        /// </summary>
        private static bool ContainsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return SqlInjectionPattern.IsMatch(input);
        }
    }
}
