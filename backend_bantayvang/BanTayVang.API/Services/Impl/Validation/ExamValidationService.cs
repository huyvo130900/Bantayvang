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
        private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ExamValidationService(
            IExamPaperRepository dethiRepository,
            IExamSubmissionRepository baithiRepository,
            IQuestionRepository cauhoiRepository,
            BanTayVangDbContext context,
            ILogger<ExamValidationService> logger)
        {
            _examPaperRepository = dethiRepository;
            _examSubmissionRepository = baithiRepository;
            _questionRepository = cauhoiRepository;
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

                if (createDto.DanhSachIdCauHoi.Count > 200)
                {
                    errors.Add("Too many questions (maximum 200 allowed)");
                }

                // Validate question IDs exist and are accessible
                if (createDto.DanhSachIdCauHoi.Any())
                {
                    var validQuestionIds = await _questionRepository.GetValidQuestionIdsAsync(createDto.DanhSachIdCauHoi);
                    var invalidIds = createDto.DanhSachIdCauHoi.Except(validQuestionIds).ToList();

                    if (invalidIds.Any())
                    {
                        errors.Add($"Invalid question IDs: {string.Join(", ", invalidIds)}");
                    }
                }

                // Time validation
                if (createDto.StartTime < DateTime.Now.AddMinutes(-10))
                {
                    warnings.Add("Start time is in the past");
                }

                return errors.Any()
                    ? ValidationResultDto.Failure(errors, "VALIDATION_FAILED")
                    : ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam creation");
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateStartExamAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // OWASP A01: Broken Access Control - Validate user permissions
                if (taikhoanId <= 0)
                {
                    errors.Add("Invalid user ID");
                    return ValidationResultDto.Failure(errors, "INVALID_USER");
                }

                // Resolve exam paper (ExamPaper)
                ExamPaper? exam = null;
                if (startDto.KyThiId.HasValue)
                {
                    exam = await _examPaperRepository.ResolveExamForCandidateAsync(startDto.KyThiId.Value, taikhoanId, cancellationToken);
                }
                else
                {
                    // OWASP A03: Injection Prevention
                    if (ContainsSqlInjection(startDto.ExamPaperCode))
                    {
                        errors.Add("Exam code contains invalid characters");
                        _logger.LogWarning("SQL injection attempt in exam start: {ExamCode} by user {UserId}", startDto.ExamPaperCode, taikhoanId);
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

                // FIX 2: Dùng DateTime.Now thay vì DateTime.UtcNow (tránh lệch 7 tiếng)
                if (exam.StartTime.HasValue && exam.StartTime > DateTime.Now)
                {
                    errors.Add("Chưa đến thời gian thi");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_STARTED");
                }

                // Check if user already completed this exam / ExamCampaign
                // Allow retakes if still within duration of ExamCampaign (or ExamPaper if standalone)
                var studentSessions = await _examSubmissionRepository.GetByTaiKhoanAsync(taikhoanId);
                bool withinDuration = false;

                if (startDto.KyThiId.HasValue)
                {
                    var examCampaign = await _context.ExamCampaigns.FindAsync(startDto.KyThiId.Value, cancellationToken);
                    if (examCampaign != null)
                    {
                        var start = examCampaign.StartTime;
                        var end = examCampaign.EndTime;
                        var now = DateTime.Now;
                        if ((!start.HasValue || now >= start.Value) && (!end.HasValue || now <= end.Value))
                        {
                            withinDuration = true;
                        }
                    }
                }
                else
                {
                    var start = exam.StartTime;
                    var duration = exam.DurationMinutes ?? 60;
                    var now = DateTime.Now;
                    if (start.HasValue && now >= start.Value && now <= start.Value.AddMinutes(duration))
                    {
                        withinDuration = true;
                    }
                }

                if (!withinDuration)
                {
                    if (startDto.KyThiId.HasValue)
                    {
                        var completedSessionInKyThi = studentSessions.Any(b => b.ExamCampaignId == startDto.KyThiId.Value 
                            && b.Status != "InProgress" && b.Status != "Paused");
                        if (completedSessionInKyThi)
                        {
                            errors.Add("Kỳ thi đã hết hạn hoặc bạn đã nộp bài thi cho kỳ thi này và không được phép thi lại.");
                            return ValidationResultDto.Failure(errors, "EXAM_ALREADY_TAKEN");
                        }
                    }

                    var completedSessionForDeThi = studentSessions.Any(b => b.ExamPaperId == exam.Id 
                        && b.Status != "InProgress" && b.Status != "Paused");
                    if (completedSessionForDeThi)
                    {
                        errors.Add("Đề thi đã hết hạn hoặc bạn đã nộp đề thi này và không được phép thi lại.");
                        return ValidationResultDto.Failure(errors, "EXAM_ALREADY_TAKEN");
                    }
                }

                // Check if user has an active session for this exam (concurrency check)
                var existingSession = await _examSubmissionRepository.GetActiveExamSessionAsync(taikhoanId, exam.Id);

                // OWASP A04: Validate concurrent session limits
                var activeSessions = await _examSubmissionRepository.GetActiveSessionsCountAsync(taikhoanId);
                if (activeSessions >= 3) // Max 3 concurrent exams
                {
                    errors.Add("Too many active exam sessions");
                    return ValidationResultDto.Failure(errors, "TOO_MANY_SESSIONS");
                }

                return ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exam start for user {UserId}", taikhoanId);
                return ValidationResultDto.Failure("Validation error occurred", "INTERNAL_ERROR");
            }
        }

        public async Task<ValidationResultDto> ValidateAnswerSubmissionAsync(SubmitAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default)
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

                if (examSession.UserId != taikhoanId)
                {
                    errors.Add("Access denied to this exam session");
                    _logger.LogWarning("Unauthorized access attempt to exam session {SessionId} by user {UserId}", answerDto.ExamSubmissionId, taikhoanId);
                    return ValidationResultDto.Failure(errors, "ACCESS_DENIED");
                }

                if (examSession.Status != "InProgress")
                {
                    errors.Add("Exam session is not active");
                    return ValidationResultDto.Failure(errors, "SESSION_INACTIVE");
                }

                // OWASP A03: Injection Prevention for essay answers
                if (!string.IsNullOrEmpty(answerDto.CauTraLoiTuLuan))
                {
                    if (ContainsSqlInjection(answerDto.CauTraLoiTuLuan))
                    {
                        errors.Add("Answer contains invalid content");
                        _logger.LogWarning("SQL injection attempt in answer submission by user {UserId}", taikhoanId);
                        return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                    }

                    // OWASP A04: Validate answer length
                    if (answerDto.CauTraLoiTuLuan.Length > 5000)
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
                        answerDto.QuestionId, answerDto.ExamSubmissionId, taikhoanId);
                    return ValidationResultDto.Failure(errors, "INVALID_QUESTION");
                }

                return ValidationResultDto.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating answer submission for user {UserId}", taikhoanId);
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

        public async Task<ValidationResultDto> ValidateExamSubmissionAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // Validate session access
                var sessionValidation = await ValidateAnswerSubmissionAsync(
                    new SubmitAnswerDto { ExamSubmissionId = submitDto.ExamSubmissionId },
                    taikhoanId,
                    cancellationToken);

                if (!sessionValidation.IsValid)
                {
                    return sessionValidation;
                }

                // Validate all answers in submission
                foreach (var answer in submitDto.DanhSachCauTraLoi)
                {
                    var answerValidation = await ValidateAnswerSubmissionAsync(answer, taikhoanId, cancellationToken);
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
                _logger.LogError(ex, "Error validating exam submission for user {UserId}", taikhoanId);
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