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
        private readonly IDethiRepository _dethiRepository;
        private readonly IBaithiRepository _baithiRepository;
        private readonly ICauhoiRepository _cauhoiRepository;
        private readonly ILogger<ExamValidationService> _logger;

        // OWASP: Define security patterns
        private static readonly Regex SafeTextPattern = new(@"^[^<>""'%;()&+]*$", RegexOptions.Compiled);
        private static readonly Regex ExamCodePattern = new(@"^[A-Z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ExamValidationService(
            IDethiRepository dethiRepository,
            IBaithiRepository baithiRepository,
            ICauhoiRepository cauhoiRepository,
            ILogger<ExamValidationService> logger)
        {
            _dethiRepository = dethiRepository;
            _baithiRepository = baithiRepository;
            _cauhoiRepository = cauhoiRepository;
            _logger = logger;
        }

        public async Task<ValidationResultDto> ValidateCreateExamAsync(CreateDethiDto createDto, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // OWASP A03: Injection Prevention
                if (ContainsSqlInjection(createDto.MaDeThi) || ContainsSqlInjection(createDto.TenDeThi))
                {
                    errors.Add("Input contains potentially malicious content");
                    _logger.LogWarning("SQL injection attempt detected in exam creation: {ExamCode}", createDto.MaDeThi);
                    return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                }

                // OWASP A03: XSS Prevention
                if (!SafeTextPattern.IsMatch(createDto.TenDeThi))
                {
                    errors.Add("Exam name contains invalid characters that could pose security risks");
                }

                if (!ExamCodePattern.IsMatch(createDto.MaDeThi))
                {
                    errors.Add("Exam code format is invalid");
                }

                // Business validation
                var existingExam = await _dethiRepository.GetByMaDeThiAsync(createDto.MaDeThi);
                if (existingExam != null)
                {
                    errors.Add("Exam code already exists");
                }

                // OWASP A04: Insecure Design - Validate reasonable limits
                if (createDto.ThoiGianLamBai > 480) // 8 hours max
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
                    var validQuestionIds = await _cauhoiRepository.GetValidQuestionIdsAsync(createDto.DanhSachIdCauHoi);
                    var invalidIds = createDto.DanhSachIdCauHoi.Except(validQuestionIds).ToList();
                    
                    if (invalidIds.Any())
                    {
                        errors.Add($"Invalid question IDs: {string.Join(", ", invalidIds)}");
                    }
                }

                // Time validation
                if (createDto.ThoiGianBatDau < DateTime.UtcNow.AddMinutes(-5))
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

                // OWASP A03: Injection Prevention
                if (ContainsSqlInjection(startDto.MaDeThi))
                {
                    errors.Add("Exam code contains invalid characters");
                    _logger.LogWarning("SQL injection attempt in exam start: {ExamCode} by user {UserId}", startDto.MaDeThi, taikhoanId);
                    return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                }

                // Validate exam exists and is active
                var exam = await _dethiRepository.GetByMaDeThiAsync(startDto.MaDeThi);
                if (exam == null)
                {
                    errors.Add("Exam not found");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_FOUND");
                }

                if (exam.TrangThai != "Active")
                {
                    errors.Add("Exam is not active");
                    return ValidationResultDto.Failure(errors, "EXAM_INACTIVE");
                }

                // Validate timing
                if (exam.ThoiGianBatDau > DateTime.UtcNow)
                {
                    errors.Add("Exam has not started yet");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_STARTED");
                }

                // Check if user already completed this exam
                var existingSession = await _baithiRepository.GetActiveExamSessionAsync(taikhoanId, exam.Id);
                if (existingSession?.TrangThai == "Completed")
                {
                    errors.Add("You have already completed this exam");
                    return ValidationResultDto.Failure(errors, "EXAM_ALREADY_COMPLETED");
                }

                // OWASP A04: Validate concurrent session limits
                var activeSessions = await _baithiRepository.GetActiveSessionsCountAsync(taikhoanId);
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
                var examSession = await _baithiRepository.GetByIdAsync(answerDto.IdBaiThi);
                if (examSession == null)
                {
                    errors.Add("Exam session not found");
                    return ValidationResultDto.Failure(errors, "SESSION_NOT_FOUND");
                }

                if (examSession.IdTaiKhoan != taikhoanId)
                {
                    errors.Add("Access denied to this exam session");
                    _logger.LogWarning("Unauthorized access attempt to exam session {SessionId} by user {UserId}", answerDto.IdBaiThi, taikhoanId);
                    return ValidationResultDto.Failure(errors, "ACCESS_DENIED");
                }

                if (examSession.TrangThai != "InProgress")
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
                var examWithQuestions = await _dethiRepository.GetWithQuestionsAsync(examSession.IdDeThi!.Value);
                var questionExists = examWithQuestions?.DethiCauhois.Any(dc => dc.IdCauHoi == answerDto.IdCauHoi) ?? false;
                
                if (!questionExists)
                {
                    errors.Add("Question does not belong to this exam");
                    _logger.LogWarning("Invalid question access attempt: Question {QuestionId} in session {SessionId} by user {UserId}", 
                        answerDto.IdCauHoi, answerDto.IdBaiThi, taikhoanId);
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
                
                // For CREATE operations, no examId check needed
                if (operation.ToUpper() == "CREATE")
                {
                    if (userId <= 0)
                    {
                        return ValidationResultDto.Failure("Invalid user", "INVALID_USER");
                    }
                    return ValidationResultDto.Success();
                }

                var exam = await _dethiRepository.GetByIdAsync(examId);
                if (exam == null)
                {
                    return ValidationResultDto.Failure("Exam not found", "EXAM_NOT_FOUND");
                }

                // Basic permission check (should be enhanced with role-based access)
                switch (operation.ToUpper())
                {
                    case "UPDATE":
                    case "DELETE":
                        // Only exam creators or admins can modify
                        if (exam.NguoiTao != userId)
                        {
                            _logger.LogWarning("Unauthorized {Operation} attempt on exam {ExamId} by user {UserId}", operation, examId, userId);
                            return ValidationResultDto.Failure("Access denied", "ACCESS_DENIED");
                        }
                        break;
                    
                    case "VIEW":
                    case "TAKE":
                        // All authenticated users can view/take active exams
                        if (exam.TrangThai != "Active" && operation == "TAKE")
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

        public async Task<ValidationResultDto> ValidateUpdateExamAsync(UpdateDethiDto updateDto, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            try
            {
                // OWASP A03: Injection Prevention
                if (ContainsSqlInjection(updateDto.MaDeThi) || ContainsSqlInjection(updateDto.TenDeThi))
                {
                    errors.Add("Input contains potentially malicious content");
                    return ValidationResultDto.Failure(errors, "SECURITY_VIOLATION");
                }

                // Validate exam exists
                var existingExam = await _dethiRepository.GetByIdAsync(updateDto.Id);
                if (existingExam == null)
                {
                    errors.Add("Exam not found");
                    return ValidationResultDto.Failure(errors, "EXAM_NOT_FOUND");
                }

                // Check if exam code is being changed and if new code exists
                if (existingExam.MaDeThi != updateDto.MaDeThi)
                {
                    var duplicateExam = await _dethiRepository.GetByMaDeThiAsync(updateDto.MaDeThi);
                    if (duplicateExam != null)
                    {
                        errors.Add("Exam code already exists");
                    }
                }

                // Validate business rules
                if (existingExam.TrangThai == "Active" && updateDto.TrangThai == "Inactive")
                {
                    // Check if there are active sessions
                    var activeSessions = await _baithiRepository.GetActiveSessionsByExamAsync(updateDto.Id);
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
                    new SubmitAnswerDto { IdBaiThi = submitDto.IdBaiThi }, 
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