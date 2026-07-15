using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl.Exams
{
    /// <summary>
    /// Exam submission service implementation following SOLID principles and OWASP security
    /// </summary>
    public class ExamSubmissionService : IExamSubmissionService
    {
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly ISubmissionDetailRepository _chitietRepository;
        private readonly IExamPaperRepository _examPaperRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamSubmissionService> _logger;

        public ExamSubmissionService(
            IExamSubmissionRepository baithiRepository,
            ISubmissionDetailRepository chitietRepository,
            IExamPaperRepository dethiRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamSubmissionService> logger)
        {
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _chitietRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _examPaperRepository = dethiRepository ?? throw new ArgumentNullException(nameof(dethiRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Saving answer for user {UserId}, session {SessionId}, question {QuestionId}",
                    taikhoanId, answerDto.ExamSubmissionId, answerDto.QuestionId);

                // OWASP A03: Injection - Input validation
                var validationResult = await ValidateAnswerAsync(answerDto, cancellationToken);
                if (!validationResult.Success)
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_VALIDATION_FAILED",
                        $"User {taikhoanId} failed answer validation for session {answerDto.ExamSubmissionId}",
                        taikhoanId, "Medium", cancellationToken);
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = validationResult.Message,
                        Errors = validationResult.Errors
                    };
                }

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(answerDto.ExamSubmissionId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_ANSWER_SUBMISSION",
                        $"User {taikhoanId} attempted unauthorized answer submission to session {answerDto.ExamSubmissionId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (examSubmission.Status != "InProgress")
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_TO_INACTIVE_EXAM",
                        $"User {taikhoanId} attempted to submit answer to inactive exam session {answerDto.ExamSubmissionId}",
                        taikhoanId, "Medium", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Bài thi đã kết thúc hoặc không ở trạng thái làm bài"
                    };
                }

                // Check if exam time has expired
                var examPaper = await _examPaperRepository.GetByIdAsync(examSubmission.ExamPaperId!.Value);
                if (IsExamExpired(examPaper, examSubmission.StartTime))
                {
                    // Auto-submit expired exam
                    await AutoSubmitExpiredExam(examSubmission, taikhoanId, cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Thời gian làm bài đã hết, bài thi đã được tự động nộp"
                    };
                }

                // OWASP A03: Injection - Sanitize input data
                var chitiet = new SubmissionDetail
                {
                    ExamSubmissionId = answerDto.ExamSubmissionId,
                    QuestionId = answerDto.QuestionId,
                    SelectedOptionId = answerDto.SelectedOptionId,
                    CauTraLoiTuLuan = SanitizeTextInput(answerDto.CauTraLoiTuLuan),
                    ThoiGianTraLoi = DateTime.Now,
                    DaLuu = answerDto.DaLuu
                };

                await _chitietRepository.SaveAnswerAsync(chitiet);

                await _securityService.LogSecurityEventAsync("ANSWER_SAVED",
                    $"User {taikhoanId} saved answer for question {answerDto.QuestionId} in session {answerDto.ExamSubmissionId}",
                    taikhoanId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Lưu câu trả lời thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving answer for user {UserId}, session {SessionId}, question {QuestionId}",
                    taikhoanId, answerDto.ExamSubmissionId, answerDto.QuestionId);

                await _securityService.LogSecurityEventAsync("ANSWER_SAVE_ERROR",
                    $"System error saving answer for user {taikhoanId}: {ex.Message}",
                    taikhoanId, "High", cancellationToken);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lưu câu trả lời",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Submitting exam for user {UserId}, session {SessionId}", taikhoanId, submitDto.ExamSubmissionId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetWithDetailsAsync(submitDto.ExamSubmissionId);
                if (examSubmission == null || examSubmission.UserId != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_EXAM_SUBMISSION",
                        $"User {taikhoanId} attempted unauthorized submission of session {submitDto.ExamSubmissionId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (examSubmission.Status == "Completed")
                {
                    await _securityService.LogSecurityEventAsync("DUPLICATE_EXAM_SUBMISSION",
                        $"User {taikhoanId} attempted duplicate submission of session {submitDto.ExamSubmissionId}",
                        taikhoanId, "Medium", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Bài thi đã được nộp trước đó"
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _examSubmissionRepository.BeginTransactionAsync();
                try
                {
                    // Nhóm danh sách câu trả lời theo QuestionId để dọn dẹp và lưu đồng bộ
                    var answersByQuestion = submitDto.DanhSachCauTraLoi
                        .GroupBy(a => a.QuestionId);

                    foreach (var group in answersByQuestion)
                    {
                        var cauhoiId = group.Key;

                        // Xóa các câu trả lời cũ/placeholder của câu hỏi này
                        await _chitietRepository.DeleteAnswersByQuestionAsync(submitDto.ExamSubmissionId, cauhoiId);

                        // Lưu các câu trả lời mới
                        foreach (var answer in group)
                        {
                            var validationResult = await ValidateAnswerAsync(answer, cancellationToken);
                            if (!validationResult.Success)
                            {
                                await _securityService.LogSecurityEventAsync("INVALID_ANSWER_IN_SUBMISSION",
                                    $"User {taikhoanId} submitted invalid answer in final submission",
                                    taikhoanId, "High", cancellationToken);
                                continue; // Bỏ qua đáp án không hợp lệ nhưng không làm dừng cả bài thi
                            }

                            var chitiet = new SubmissionDetail
                            {
                                ExamSubmissionId = answer.ExamSubmissionId,
                                QuestionId = answer.QuestionId,
                                SelectedOptionId = answer.SelectedOptionId,
                                CauTraLoiTuLuan = SanitizeTextInput(answer.CauTraLoiTuLuan),
                                ThoiGianTraLoi = DateTime.Now,
                                DaLuu = true
                            };

                            await _chitietRepository.AddAsync(chitiet);
                        }
                    }

                    // Grade the exam automatically (MCQs graded, Essays left ungraded)
                    var (correctAnswers, totalScore) = await GradeExamAsync(submitDto.ExamSubmissionId);

                    // Update exam status
                    examSubmission.Status = "Completed";
                    examSubmission.SubmitTime = DateTime.Now;
                    examSubmission.CorrectAnswers = correctAnswers;
                    examSubmission.TotalScore = totalScore;

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    await _securityService.LogSecurityEventAsync("EXAM_SUBMITTED",
                        $"User {taikhoanId} successfully submitted exam session {submitDto.ExamSubmissionId} with score {totalScore}",
                        taikhoanId, "Info", cancellationToken);

                    var result = _mapper.Map<ExamSubmissionDto>(examSubmission);
                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = true,
                        Message = "Nộp bài thành công",
                        Data = result
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting exam for user {UserId}, session {SessionId}", taikhoanId, submitDto.ExamSubmissionId);

                await _securityService.LogSecurityEventAsync("EXAM_SUBMISSION_ERROR",
                    $"System error during exam submission for user {taikhoanId}: {ex.Message}",
                    taikhoanId, "High", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi nộp bài",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> AutoSubmitExpiredExamsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting auto-submit process for expired exams");

                var expiredExams = await _examSubmissionRepository.GetExpiredInProgressExamsAsync();
                int submittedCount = 0;

                foreach (var examSubmission in expiredExams)
                {
                    try
                    {
                        await AutoSubmitExpiredExam(examSubmission, examSubmission.UserId!.Value, cancellationToken);
                        submittedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-submitting exam session {SessionId}", examSubmission.Id);

                        await _securityService.LogSecurityEventAsync("AUTO_SUBMIT_ERROR",
                            $"Error auto-submitting session {examSubmission.Id}: {ex.Message}",
                            examSubmission.UserId!.Value, "Medium", cancellationToken);
                    }
                }

                _logger.LogInformation("Auto-submitted {Count} expired exams", submittedCount);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = $"Đã tự động nộp {submittedCount} bài thi hết hạn"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-submit process");

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình tự động nộp bài",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<bool>> ValidateAnswerAsync(SubmitAnswerDto answerDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // OWASP A03: Injection - Input validation
                if (answerDto.ExamSubmissionId <= 0)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "ID bài thi không hợp lệ",
                        Data = false
                    };
                }

                if (answerDto.QuestionId <= 0)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "ID câu hỏi không hợp lệ",
                        Data = false
                    };
                }

                // Validate text input length (OWASP A04: Insecure Design)
                if (!string.IsNullOrEmpty(answerDto.CauTraLoiTuLuan) && answerDto.CauTraLoiTuLuan.Length > 5000)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "Câu trả lời tự luận quá dài (tối đa 5000 ký tự)",
                        Data = false
                    };
                }

                // Validate choice ID if provided
                if (answerDto.SelectedOptionId.HasValue && answerDto.SelectedOptionId.Value <= 0)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "ID lựa chọn không hợp lệ",
                        Data = false
                    };
                }

                // Check for malicious content (basic XSS prevention)
                if (ContainsMaliciousContent(answerDto.CauTraLoiTuLuan))
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "Nội dung câu trả lời chứa ký tự không được phép",
                        Data = false
                    };
                }

                return new BaseResponseDto<bool>
                {
                    Success = true,
                    Message = "Câu trả lời hợp lệ",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating answer");

                return new BaseResponseDto<bool>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi kiểm tra câu trả lời",
                    Data = false
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Automatically grades the multiple-choice questions (MCQs) of an exam and leaves essay questions ungraded (null)
        /// </summary>
        private async Task<(int CorrectAnswers, double TotalScore)> GradeExamAsync(int baithiId)
        {
            var answers = await _chitietRepository.GetByBaiThiAsync(baithiId);
            int correctAnswers = 0;
            var answersByQuestion = answers.Where(c => c.QuestionId.HasValue).GroupBy(c => c.QuestionId!.Value);

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
                    // For essays, leave ScoreObtained as null (or whatever the user has manually graded)
                    var existingScore = group.FirstOrDefault()?.ScoreObtained;
                    foreach (var ct in group)
                    {
                        ct.ScoreObtained = existingScore;
                    }
                    if (existingScore >= 1) correctAnswers++;
                }
                else
                {
                    var userChoiceIds = group
                        .Where(c => c.SelectedOptionId.HasValue)
                        .Select(c => c.SelectedOptionId!.Value)
                        .ToHashSet();

                    bool isFullyCorrect = correctChoiceIds.Count > 0 && correctChoiceIds.SetEquals(userChoiceIds);

                    foreach (var ct in group)
                    {
                        ct.ScoreObtained = isFullyCorrect ? (1.0 / Math.Max(1, group.Count())) : 0.0;
                    }
                    if (isFullyCorrect) correctAnswers++;
                }
            }
            return (correctAnswers, correctAnswers);
        }

        /// <summary>
        /// Auto-submit an expired exam
        /// </summary>
        private async Task AutoSubmitExpiredExam(ExamSubmission examSubmission, int taikhoanId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Auto-submitting expired exam session {SessionId} for user {UserId}", examSubmission.Id, taikhoanId);

            // Grade exam automatically
            var (correctAnswers, totalScore) = await GradeExamAsync(examSubmission.Id);

            // Update exam status
            examSubmission.Status = "Completed";
            examSubmission.SubmitTime = DateTime.Now;
            examSubmission.CorrectAnswers = correctAnswers;
            examSubmission.TotalScore = totalScore;

            await _context.SaveChangesAsync(cancellationToken);

            await _securityService.LogSecurityEventAsync("EXAM_AUTO_SUBMITTED",
                $"Exam session {examSubmission.Id} auto-submitted for user {taikhoanId} due to time expiry",
                taikhoanId, "Info", cancellationToken);
        }

        /// <summary>
        /// Check if exam time has expired
        /// </summary>
        private bool IsExamExpired(ExamPaper? examPaper, DateTime? sessionStartTime)
        {
            if (examPaper?.DurationMinutes == null || sessionStartTime == null)
                return false;

            var examDurationMinutes = examPaper.DurationMinutes.Value;
            var examEndTime = sessionStartTime.Value.AddMinutes(examDurationMinutes);

            return DateTime.Now > examEndTime;
        }

        /// <summary>
        /// Calculate exam score with business rules (thang điểm 10)
        /// Ví dụ: đúng 30/60 câu = (30/60)*10 = 5.0 điểm
        /// </summary>
        private double CalculateScore(int correctAnswers, int totalQuestions)
        {
            return correctAnswers;
        }

        /// <summary>
        /// OWASP A03: Injection - Sanitize text input to prevent XSS and injection
        /// </summary>
        private string? SanitizeTextInput(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Basic sanitization - in production, use a proper sanitizer
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

        /// <summary>
        /// Check for malicious content in user input
        /// </summary>
        private bool ContainsMaliciousContent(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var maliciousPatterns = new[]
            {
                "<script",
                "javascript:",
                "vbscript:",
                "onload=",
                "onerror=",
                "onclick=",
                "eval(",
                "expression(",
                "url(",
                "import(",
                "document.cookie",
                "document.write"
            };

            var lowerInput = input.ToLowerInvariant();
            return maliciousPatterns.Any(pattern => lowerInput.Contains(pattern));
        }

        #endregion
    }
}



