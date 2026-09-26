using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
using BanTayVang.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl.Exams
{
    /// <summary>
    /// Exam submission service implementation following SOLID principles and OWASP security
    /// </summary>
    public class ExamSubmissionService : IExamSubmissionService
    {
        private readonly IExamSubmissionRepository _examSubmissionRepository;
        private readonly ISubmissionDetailRepository _submissionDetailRepository;
        private readonly IExamPaperRepository _examPaperRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamSubmissionService> _logger;
        private readonly IExamCampaignService _examCampaignService;
        private readonly IAuditLogService _auditLogService;
        private readonly Hubs.IExamMonitorNotifier _examMonitorNotifier;

        public ExamSubmissionService(
            IExamSubmissionRepository baithiRepository,
            ISubmissionDetailRepository chitietRepository,
            IExamPaperRepository examPaperRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamSubmissionService> logger,
            IExamCampaignService examCampaignService,
            IAuditLogService auditLogService,
            Hubs.IExamMonitorNotifier examMonitorNotifier)
        {
            _examSubmissionRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _submissionDetailRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _examPaperRepository = examPaperRepository ?? throw new ArgumentNullException(nameof(examPaperRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _examCampaignService = examCampaignService ?? throw new ArgumentNullException(nameof(examCampaignService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _examMonitorNotifier = examMonitorNotifier ?? throw new ArgumentNullException(nameof(examMonitorNotifier));
        }

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Saving answer for user {UserId}, session {SessionId}, question {QuestionId}",
                    userId, answerDto.ExamSubmissionId, answerDto.QuestionId);

                // OWASP A03: Injection - Input validation
                var validationResult = await ValidateAnswerAsync(answerDto, cancellationToken);
                if (!validationResult.Success)
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_VALIDATION_FAILED",
                        $"User {userId} failed answer validation for session {answerDto.ExamSubmissionId}",
                        userId, "Medium", cancellationToken);
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = validationResult.Message,
                        Errors = validationResult.Errors
                    };
                }

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetByIdAsync(answerDto.ExamSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_ANSWER_SUBMISSION",
                        $"User {userId} attempted unauthorized answer submission to session {answerDto.ExamSubmissionId}",
                        userId, "High", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (examSubmission.Status != "InProgress")
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_TO_INACTIVE_EXAM",
                        $"User {userId} attempted to submit answer to inactive exam session {answerDto.ExamSubmissionId}",
                        userId, "Medium", cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Bài thi đã kết thúc hoặc không ở trạng thái làm bài"
                    };
                }

                // BUG FIX: ExamCampaign.Status ("TamDung"/"DaKetThuc", set via the admin/DeptManager
                // "change status" dropdown) used to never be checked anywhere in this save-answer
                // path, so a student already mid-exam kept saving/submitting answers normally even
                // after the campaign was paused or manually ended - the button had zero real effect.
                if (examSubmission.ExamCampaignId.HasValue)
                {
                    var campaignStatus = await _context.ExamCampaigns
                        .Where(c => c.Id == examSubmission.ExamCampaignId.Value)
                        .Select(c => c.Status)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (campaignStatus == "TamDung" || campaignStatus == "DaKetThuc")
                    {
                        return new BaseResponseDto
                        {
                            Success = false,
                            Message = "Kỳ thi đã bị tạm dừng hoặc kết thúc, không thể tiếp tục làm bài"
                        };
                    }
                }

                // Check if exam time has expired
                var examPaper = await _examPaperRepository.GetByIdAsync(examSubmission.ExamPaperId!.Value);
                if (IsExamExpired(examPaper, examSubmission.StartTime, await GetExtraMinutesAsync(userId, examSubmission.ExamPaperId)))
                {
                    // Auto-submit expired exam
                    await AutoSubmitExpiredExam(examSubmission, userId, cancellationToken);

                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Thời gian làm bài đã hết, bài thi đã được tự động nộp"
                    };
                }

                // OWASP A03: Injection - Sanitize input data
                var detail = new SubmissionDetail
                {
                    ExamSubmissionId = answerDto.ExamSubmissionId,
                    QuestionId = answerDto.QuestionId,
                    SelectedOptionId = answerDto.SelectedOptionId,
                    EssayAnswer = SanitizeTextInput(answerDto.EssayAnswer),
                    EssayImageUrl = answerDto.EssayImageUrl,
                    AnswerTime = DateTime.UtcNow.AddHours(7),
                    IsSaved = answerDto.IsSaved
                };

                await _submissionDetailRepository.SaveAnswerAsync(detail);

                await _securityService.LogSecurityEventAsync("ANSWER_SAVED",
                    $"User {userId} saved answer for question {answerDto.QuestionId} in session {answerDto.ExamSubmissionId}",
                    userId, "Info", cancellationToken);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Lưu câu trả lời thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving answer for user {UserId}, session {SessionId}, question {QuestionId}",
                    userId, answerDto.ExamSubmissionId, answerDto.QuestionId);

                await _securityService.LogSecurityEventAsync("ANSWER_SAVE_ERROR",
                    $"System error saving answer for user {userId}: {ex.Message}",
                    userId, "High", cancellationToken);

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lưu câu trả lời",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<ExamSubmissionDto>> SubmitExamAsync(SubmitExamDto submitDto, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Submitting exam for user {UserId}, session {SessionId}", userId, submitDto.ExamSubmissionId);

                // OWASP A01: Broken Access Control - Verify ownership
                var examSubmission = await _examSubmissionRepository.GetWithDetailsAsync(submitDto.ExamSubmissionId);
                if (examSubmission == null || examSubmission.UserId != userId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_EXAM_SUBMISSION",
                        $"User {userId} attempted unauthorized submission of session {submitDto.ExamSubmissionId}",
                        userId, "High", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (examSubmission.Status == "Completed")
                {
                    await _securityService.LogSecurityEventAsync("DUPLICATE_EXAM_SUBMISSION",
                        $"User {userId} attempted duplicate submission of session {submitDto.ExamSubmissionId}",
                        userId, "Medium", cancellationToken);

                    return new BaseResponseDto<ExamSubmissionDto>
                    {
                        Success = false,
                        Message = "Bài thi đã được nộp trước đó"
                    };
                }

                // BUG FIX: same gap as SaveAnswerAsync - final submission never checked
                // ExamCampaign.Status, so a paused/manually-ended campaign had no effect on a
                // student's ability to submit an in-progress exam.
                if (examSubmission.ExamCampaignId.HasValue)
                {
                    var campaignStatus = await _context.ExamCampaigns
                        .Where(c => c.Id == examSubmission.ExamCampaignId.Value)
                        .Select(c => c.Status)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (campaignStatus == "TamDung" || campaignStatus == "DaKetThuc")
                    {
                        return new BaseResponseDto<ExamSubmissionDto>
                        {
                            Success = false,
                            Message = "Kỳ thi đã bị tạm dừng hoặc kết thúc, không thể nộp bài"
                        };
                    }
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _examSubmissionRepository.BeginTransactionAsync();
                try
                {
                    // BUG FIX: nothing here previously stopped this manual submit from racing the
                    // background AutoSubmitExpiredExamsJob (runs ~every minute) or a concurrent
                    // ForceSubmitAsync call for the SAME submission - the Status=="Completed" check
                    // done earlier in this method is a stale snapshot, not a lock. If the exam
                    // duration elapses at almost the exact moment the student clicks "Nộp bài", both
                    // paths could independently grade and write CorrectAnswers/TotalScore, with
                    // whichever SaveChanges/commit lands last silently overwriting the other's score.
                    // Atomically claim the row first (only one caller can flip InProgress->Completed);
                    // the loser backs off instead of finalizing a submission someone else already did.
                    var claimed = await _context.ExamSubmissions
                        .Where(e => e.Id == submitDto.ExamSubmissionId && e.Status == "InProgress")
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "Completed"), cancellationToken);

                    if (claimed == 0)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return new BaseResponseDto<ExamSubmissionDto>
                        {
                            Success = false,
                            Message = "Bài thi đã được nộp trước đó"
                        };
                    }

                    // Nhóm danh sách câu trả lời theo QuestionId để dọn dẹp và lưu đồng bộ
                    var answersByQuestion = submitDto.Answers
                        .GroupBy(a => a.QuestionId);

                    foreach (var group in answersByQuestion)
                    {
                        var questionId = group.Key;

                        // Xóa các câu trả lời cũ/placeholder của câu hỏi này
                        await _submissionDetailRepository.DeleteAnswersByQuestionAsync(submitDto.ExamSubmissionId, questionId);

                        // Lưu các câu trả lời mới
                        foreach (var answer in group)
                        {
                            var validationResult = await ValidateAnswerAsync(answer, cancellationToken);
                            if (!validationResult.Success)
                            {
                                await _securityService.LogSecurityEventAsync("INVALID_ANSWER_IN_SUBMISSION",
                                    $"User {userId} submitted invalid answer in final submission",
                                    userId, "High", cancellationToken);
                                continue; // Bỏ qua đáp án không hợp lệ nhưng không làm dừng cả bài thi
                            }

                            var detail = new SubmissionDetail
                            {
                                ExamSubmissionId = answer.ExamSubmissionId,
                                QuestionId = answer.QuestionId,
                                SelectedOptionId = answer.SelectedOptionId,
                                EssayAnswer = SanitizeTextInput(answer.EssayAnswer),
                                EssayImageUrl = answer.EssayImageUrl,
                                AnswerTime = DateTime.UtcNow.AddHours(7),
                                IsSaved = true
                            };

                            await _submissionDetailRepository.AddAsync(detail);
                        }
                    }

                    // Grade the exam automatically (MCQs graded, Essays left ungraded)
                    var (correctAnswers, totalScore) = await GradeExamAsync(submitDto.ExamSubmissionId);

                    // Update exam status
                    examSubmission.Status = "Completed";
                    examSubmission.SubmitTime = DateTime.UtcNow.AddHours(7);
                    examSubmission.CorrectAnswers = correctAnswers;
                    examSubmission.TotalScore = totalScore;

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    await _securityService.LogSecurityEventAsync("EXAM_SUBMITTED",
                        $"User {userId} successfully submitted exam session {submitDto.ExamSubmissionId} with score {totalScore}",
                        userId, "Info", cancellationToken);

                    // [FIX] Thiếu tín hiệu này -> Giám thị không biết thí sinh đã tự nộp bài,
                    // thẻ vẫn hiển thị "đang thi" cho tới khi F5 lại trang.
                    if (examSubmission.ExamCampaignId.HasValue)
                    {
                        await _examMonitorNotifier.NotifyExamSubmitted(
                            examSubmission.ExamCampaignId.Value,
                            examSubmission.Id,
                            examSubmission.User?.FullName ?? "Unknown",
                            totalScore);
                    }

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
                _logger.LogError(ex, "Error submitting exam for user {UserId}, session {SessionId}", userId, submitDto.ExamSubmissionId);

                await _securityService.LogSecurityEventAsync("EXAM_SUBMISSION_ERROR",
                    $"System error during exam submission for user {userId}: {ex.Message}",
                    userId, "High", cancellationToken);

                return new BaseResponseDto<ExamSubmissionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi nộp bài",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> ForceSubmitAsync(int examSubmissionId, int? supervisorId, int? supervisorDeptId, string? supervisorDeptName = null, bool isDeptManager = false)
        {
            var examSubmission = await _examSubmissionRepository.GetByIdAsync(examSubmissionId);
            if (examSubmission == null)
                return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };

            if (examSubmission.Status != "InProgress")
                return new BaseResponseDto { Success = false, Message = "Bài thi không ở trạng thái đang làm, không thể buộc nộp" };

            // BUG FIX: this only checked ownership via examSubmission.ExamCampaignId, so a
            // standalone exam paper not linked to any campaign (ExamCampaignId is optional - see
            // CreateExamPaperDto) skipped the department check entirely, letting any DeptManager
            // force-submit any student's session regardless of department. Fall back to the same
            // User/ExamPaper name-based ownership check used everywhere else in the codebase
            // (GradingController, etc.) when there's no campaign to check against.
            //
            // BUG FIX 2: `if (supervisorDeptId.HasValue)` also skipped this ENTIRE block whenever a
            // DeptManager's managed_department_id claim was empty (an account created via the
            // single-user API path, which doesn't require a department the way bulk import does) -
            // silently allowing that account to force-submit ANY student's session in ANY
            // department. `isDeptManager` (passed explicitly by the controller, since a null
            // supervisorDeptId is otherwise indistinguishable from "system auto-lock, no
            // supervisor at all") makes this fail-closed instead.
            if (isDeptManager)
            {
                if (supervisorDeptId == null)
                    return new BaseResponseDto { Success = false, Message = "Tài khoản quản lý khoa chưa được gán Khoa/Phòng để quản lý." };

                bool isOwner = examSubmission.User?.Department == supervisorDeptName ||
                               examSubmission.ExamPaper?.Department == supervisorDeptName;

                if (!isOwner && examSubmission.ExamCampaignId.HasValue)
                {
                    var campaignResult = await _examCampaignService.GetByIdAsync(examSubmission.ExamCampaignId.Value);
                    isOwner = campaignResult.Data?.DepartmentIds.Contains(supervisorDeptId.Value) == true;
                }

                if (!isOwner)
                    return new BaseResponseDto { Success = false, Message = "Bạn không có quyền với bài thi thuộc khoa khác" };
            }

            await AutoSubmitExpiredExam(examSubmission, examSubmission.UserId!.Value, CancellationToken.None);

            // [FIX v2] supervisorId == null nghĩa là hệ thống tự khóa do vượt ngưỡng cảnh báo
            // (xem ExamService.LogSuspiciousActivityAsync), không phải giám thị bấm nút "Đuổi thi".
            // Ghi log rõ ràng để phân biệt 2 nguồn gốc, tránh nhầm audit trail.
            var actionType = supervisorId.HasValue ? "EXAM_FORCE_SUBMITTED_BY_SUPERVISOR" : "EXAM_AUTO_LOCKED_BY_SYSTEM";
            var actionDescription = supervisorId.HasValue
                ? $"Giám thị (userId={supervisorId}) buộc nộp bài thi submission #{examSubmissionId}"
                : $"Hệ thống tự động khóa bài thi submission #{examSubmissionId} do vượt ngưỡng cảnh báo gian lận";

            await _securityService.LogSecurityEventAsync(
                actionType, actionDescription, supervisorId, "High", CancellationToken.None);

            await _auditLogService.LogActionAsync(
                actionType: actionType,
                description: actionDescription,
                userId: supervisorId,
                examSubmissionId: examSubmissionId);

            await _examMonitorNotifier.NotifyForceSubmitTriggered(examSubmissionId);

            // [FIX] Trước đây thiếu tín hiệu này -> thẻ thí sinh bên màn hình Giám thị bị kẹt ở
            // trạng thái đỏ/đang thi cho tới khi F5 lại trang. AutoSubmitExpiredExam đã set
            // examSubmission.Status/TotalScore trên cùng instance này (tracked by EF) nên dùng lại luôn.
            if (examSubmission.ExamCampaignId.HasValue)
            {
                await _examMonitorNotifier.NotifyExamSubmitted(
                    examSubmission.ExamCampaignId.Value,
                    examSubmissionId,
                    examSubmission.User?.FullName ?? "Unknown",
                    examSubmission.TotalScore ?? 0);
            }

            return new BaseResponseDto { Success = true, Message = "Đã buộc nộp bài thành công" };
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

                        // [FIX v2] Trước đây thiếu tín hiệu này -> giám thị nhìn thấy thẻ thí sinh kẹt ở
                        // trạng thái "Đang thi" mãi dù DB đã Completed do hết giờ, phải F5 mới cập nhật.
                        if (examSubmission.ExamCampaignId.HasValue)
                        {
                            await _examMonitorNotifier.NotifyExamSubmitted(
                                examSubmission.ExamCampaignId.Value,
                                examSubmission.Id,
                                examSubmission.User?.FullName ?? "Unknown",
                                examSubmission.TotalScore ?? 0);
                        }
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
                if (!string.IsNullOrEmpty(answerDto.EssayAnswer) && answerDto.EssayAnswer.Length > 5000)
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
                if (ContainsMaliciousContent(answerDto.EssayAnswer))
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "Nội dung câu trả lời chứa ký tự không được phép",
                        Data = false
                    };
                }

                // BUG FIX (OWASP A01/A03): verify the question actually belongs to this
                // submission's exam paper, and (if a choice was selected) that the choice
                // actually belongs to that question. Without this, a client could save an
                // answer row referencing a QuestionId/SelectedOptionId that has nothing to
                // do with the student's own exam paper - a data-integrity/broken-access-
                // control gap, even though it does not directly inflate the final score.
                var submissionExamPaperId = await _context.ExamSubmissions
                    .Where(b => b.Id == answerDto.ExamSubmissionId)
                    .Select(b => b.ExamPaperId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (submissionExamPaperId == null)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy bài thi",
                        Data = false
                    };
                }

                var questionBelongsToPaper = await _context.ExamPaperQuestions
                    .AnyAsync(dc => dc.ExamPaperId == submissionExamPaperId.Value && dc.QuestionId == answerDto.QuestionId, cancellationToken);
                if (!questionBelongsToPaper)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "Câu hỏi không thuộc đề thi này",
                        Data = false
                    };
                }

                if (answerDto.SelectedOptionId.HasValue)
                {
                    var optionBelongsToQuestion = await _context.QuestionOptions
                        .AnyAsync(o => o.Id == answerDto.SelectedOptionId.Value && o.QuestionId == answerDto.QuestionId, cancellationToken);
                    if (!optionBelongsToQuestion)
                    {
                        return new BaseResponseDto<bool>
                        {
                            Success = false,
                            Message = "Lựa chọn không thuộc câu hỏi này",
                            Data = false
                        };
                    }
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
        /// Tinh diem quy doi thang 10
        /// </summary>
        public static double CalculateTotalScore(double sumScoreObtained, int totalQuestions)
        {
            if (totalQuestions <= 0) return 0;
            return Math.Round(sumScoreObtained * 10.0 / totalQuestions, 2);
        }

        /// <summary>
        /// Automatically grades the multiple-choice questions (MCQs) of an exam and leaves essay questions ungraded (null)
        /// </summary>
        private async Task<(int CorrectAnswers, double TotalScore)> GradeExamAsync(int examSubmissionId)
        {
            var examSubmission = await _context.ExamSubmissions.FirstOrDefaultAsync(e => e.Id == examSubmissionId);
            var totalQuestions = examSubmission?.TotalQuestions ?? 0;
            var answers = await _submissionDetailRepository.GetByBaiThiAsync(examSubmissionId);
            int correctAnswers = 0;
            double sumScore = 0;
            var answersByQuestion = answers.Where(c => c.QuestionId.HasValue).GroupBy(c => c.QuestionId!.Value);

            foreach (var group in answersByQuestion)
            {
                var question = group.First().Question;
                if (question == null) continue;

                var correctChoiceIds = question.QuestionOptions
                    .Where(l => l.IsCorrect == true)
                    .Select(l => l.Id)
                    .ToHashSet();

                var categoryName = question.QuestionCategory?.CategoryName;
                var description = question.QuestionCategory?.Description;
                var isEssay = EssayQuestionHelper.IsEssay(question);

                if (isEssay)
                {
                    // For essays, leave ScoreObtained as null (or whatever the user has manually graded)
                    var existingScore = group.FirstOrDefault()?.ScoreObtained;
                    foreach (var ct in group)
                    {
                        ct.ScoreObtained = existingScore;
                    }
                    if (existingScore > 0) correctAnswers++;
                    sumScore += existingScore ?? 0;
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
                    if (isFullyCorrect) 
                    {
                        correctAnswers++;
                        sumScore += 1;
                    }
                }
            }
            var totalScore = CalculateTotalScore(sumScore, totalQuestions);
            return (correctAnswers, totalScore);
        }

        /// <summary>
        /// Auto-submit an expired exam
        /// </summary>
        private async Task AutoSubmitExpiredExam(ExamSubmission examSubmission, int userId, CancellationToken cancellationToken)
        {
            // BUG FIX: same race as SubmitExamAsync (see comment there) but from the other side -
            // this is called from SaveAnswerAsync (a student's own answer-save detecting expiry),
            // ForceSubmitAsync, and the AutoSubmitExpiredExamsJob background job, so several of
            // these paths could reach here for the SAME submission at nearly the same moment.
            // Claim the row atomically first; whoever loses the race backs off instead of grading
            // and overwriting a submission another path already finalized.
            var claimed = await _context.ExamSubmissions
                .Where(e => e.Id == examSubmission.Id && e.Status == "InProgress")
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "Completed"), cancellationToken);

            if (claimed == 0)
            {
                _logger.LogInformation("Exam session {SessionId} was already finalized by another process - skipping auto-submit", examSubmission.Id);
                return;
            }

            _logger.LogInformation("Auto-submitting expired exam session {SessionId} for user {UserId}", examSubmission.Id, userId);

            // Grade exam automatically
            var (correctAnswers, totalScore) = await GradeExamAsync(examSubmission.Id);

            // Update exam status
            examSubmission.Status = "Completed";
            examSubmission.SubmitTime = DateTime.UtcNow.AddHours(7);
            examSubmission.CorrectAnswers = correctAnswers;
            examSubmission.TotalScore = totalScore;

            await _context.SaveChangesAsync(cancellationToken);

            await _securityService.LogSecurityEventAsync("EXAM_AUTO_SUBMITTED",
                $"Exam session {examSubmission.Id} auto-submitted for user {userId} due to time expiry",
                userId, "Info", cancellationToken);
        }

        /// <summary>
        /// Check if exam time has expired
        /// </summary>
        private bool IsExamExpired(ExamPaper? examPaper, DateTime? sessionStartTime, int extraMinutes = 0)
        {
            if (examPaper?.DurationMinutes == null || sessionStartTime == null)
                return false;

            var examDurationMinutes = examPaper.DurationMinutes.Value + extraMinutes;
            var examEndTime = sessionStartTime.Value.AddMinutes(examDurationMinutes);

            var nowVN = DateTime.UtcNow.AddHours(7);
            return nowVN > examEndTime;
        }

        // BUG FIX: ExamAssignment.ExtraMinutes (supervisor "extend-time" grant) used to never be
        // read here, so a student's own answer-save would trip IsExamExpired -> AutoSubmitExpiredExam
        // at the ORIGINAL duration even after being granted extra time.
        private async Task<int> GetExtraMinutesAsync(int userId, int? examPaperId)
        {
            if (examPaperId == null) return 0;
            return await _context.ExamAssignments
                .Where(a => a.UserId == userId && a.ExamId == examPaperId.Value && a.IsActive)
                .Select(a => a.ExtraMinutes ?? 0)
                .FirstOrDefaultAsync();
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



