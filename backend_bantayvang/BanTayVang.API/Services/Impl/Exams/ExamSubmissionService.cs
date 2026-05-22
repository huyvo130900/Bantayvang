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
        private readonly IBaithiRepository _baithiRepository;
        private readonly IChitietlambaiRepository _chitietRepository;
        private readonly IDethiRepository _dethiRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly ILogger<ExamSubmissionService> _logger;

        public ExamSubmissionService(
            IBaithiRepository baithiRepository,
            IChitietlambaiRepository chitietRepository,
            IDethiRepository dethiRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            ILogger<ExamSubmissionService> logger)
        {
            _baithiRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _chitietRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _dethiRepository = dethiRepository ?? throw new ArgumentNullException(nameof(dethiRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Saving answer for user {UserId}, session {SessionId}, question {QuestionId}", 
                    taikhoanId, answerDto.IdBaiThi, answerDto.IdCauHoi);

                // OWASP A03: Injection - Input validation
                var validationResult = await ValidateAnswerAsync(answerDto, cancellationToken);
                if (!validationResult.Success)
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_VALIDATION_FAILED", 
                        $"User {taikhoanId} failed answer validation for session {answerDto.IdBaiThi}", 
                        taikhoanId, "Medium", cancellationToken);
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = validationResult.Message,
                        Errors = validationResult.Errors
                    };
                }

                // OWASP A01: Broken Access Control - Verify ownership
                var baithi = await _baithiRepository.GetByIdAsync(answerDto.IdBaiThi);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_ANSWER_SUBMISSION", 
                        $"User {taikhoanId} attempted unauthorized answer submission to session {answerDto.IdBaiThi}", 
                        taikhoanId, "High", cancellationToken);
                    
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (baithi.TrangThai != "InProgress")
                {
                    await _securityService.LogSecurityEventAsync("ANSWER_TO_INACTIVE_EXAM", 
                        $"User {taikhoanId} attempted to submit answer to inactive exam session {answerDto.IdBaiThi}", 
                        taikhoanId, "Medium", cancellationToken);
                    
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Bài thi đã kết thúc hoặc không ở trạng thái làm bài"
                    };
                }

                // Check if exam time has expired
                var dethi = await _dethiRepository.GetByIdAsync(baithi.IdDeThi!.Value);
                if (IsExamExpired(dethi, baithi.ThoiGianBatDau))
                {
                    // Auto-submit expired exam
                    await AutoSubmitExpiredExam(baithi, taikhoanId, cancellationToken);
                    
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Thời gian làm bài đã hết, bài thi đã được tự động nộp"
                    };
                }

                // OWASP A03: Injection - Sanitize input data
                var chitiet = new Chitietlambai
                {
                    IdBaiThi = answerDto.IdBaiThi,
                    IdCauHoi = answerDto.IdCauHoi,
                    IdLuaChonDaChon = answerDto.IdLuaChonDaChon,
                    CauTraLoiTuLuan = SanitizeTextInput(answerDto.CauTraLoiTuLuan),
                    ThoiGianTraLoi = DateTime.Now,
                    DaLuu = answerDto.DaLuu
                };

                await _chitietRepository.SaveAnswerAsync(chitiet);

                await _securityService.LogSecurityEventAsync("ANSWER_SAVED", 
                    $"User {taikhoanId} saved answer for question {answerDto.IdCauHoi} in session {answerDto.IdBaiThi}", 
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
                    taikhoanId, answerDto.IdBaiThi, answerDto.IdCauHoi);
                
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

        public async Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Submitting exam for user {UserId}, session {SessionId}", taikhoanId, submitDto.IdBaiThi);

                // OWASP A01: Broken Access Control - Verify ownership
                var baithi = await _baithiRepository.GetWithDetailsAsync(submitDto.IdBaiThi);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_EXAM_SUBMISSION", 
                        $"User {taikhoanId} attempted unauthorized submission of session {submitDto.IdBaiThi}", 
                        taikhoanId, "High", cancellationToken);
                    
                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                if (baithi.TrangThai == "Completed")
                {
                    await _securityService.LogSecurityEventAsync("DUPLICATE_EXAM_SUBMISSION", 
                        $"User {taikhoanId} attempted duplicate submission of session {submitDto.IdBaiThi}", 
                        taikhoanId, "Medium", cancellationToken);
                    
                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = "Bài thi đã được nộp trước đó"
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _baithiRepository.BeginTransactionAsync();
                try
                {
                    // Save all final answers with validation
                    foreach (var answer in submitDto.DanhSachCauTraLoi)
                    {
                        var validationResult = await ValidateAnswerAsync(answer, cancellationToken);
                        if (!validationResult.Success)
                        {
                            await _securityService.LogSecurityEventAsync("INVALID_ANSWER_IN_SUBMISSION", 
                                $"User {taikhoanId} submitted invalid answer in final submission", 
                                taikhoanId, "High", cancellationToken);
                            continue; // Skip invalid answers but don't fail entire submission
                        }

                        var chitiet = new Chitietlambai
                        {
                            IdBaiThi = answer.IdBaiThi,
                            IdCauHoi = answer.IdCauHoi,
                            IdLuaChonDaChon = answer.IdLuaChonDaChon,
                            CauTraLoiTuLuan = SanitizeTextInput(answer.CauTraLoiTuLuan),
                            ThoiGianTraLoi = DateTime.Now,
                            DaLuu = true
                        };

                        await _chitietRepository.SaveAnswerAsync(chitiet);
                    }

                    // Calculate score with integrity check
                    var soCauDung = await _chitietRepository.CountCorrectAnswersAsync(submitDto.IdBaiThi);
                    var tongDiem = CalculateScore(soCauDung, baithi.TongSoCau ?? 0);

                    // Update exam status
                    baithi.TrangThai = "Completed";
                    baithi.ThoiGianNop = DateTime.Now;
                    baithi.SoCauDung = soCauDung;
                    baithi.TongDiem = tongDiem;

                    await _baithiRepository.UpdateAsync(baithi);
                    await transaction.CommitAsync(cancellationToken);

                    await _securityService.LogSecurityEventAsync("EXAM_SUBMITTED", 
                        $"User {taikhoanId} successfully submitted exam session {submitDto.IdBaiThi} with score {tongDiem}", 
                        taikhoanId, "Info", cancellationToken);

                    var result = _mapper.Map<BaithiDto>(baithi);
                    return new BaseResponseDto<BaithiDto>
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
                _logger.LogError(ex, "Error submitting exam for user {UserId}, session {SessionId}", taikhoanId, submitDto.IdBaiThi);
                
                await _securityService.LogSecurityEventAsync("EXAM_SUBMISSION_ERROR", 
                    $"System error during exam submission for user {taikhoanId}: {ex.Message}", 
                    taikhoanId, "High", cancellationToken);

                return new BaseResponseDto<BaithiDto>
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

                var expiredExams = await _baithiRepository.GetExpiredInProgressExamsAsync();
                int submittedCount = 0;

                foreach (var baithi in expiredExams)
                {
                    try
                    {
                        await AutoSubmitExpiredExam(baithi, baithi.IdTaiKhoan!.Value, cancellationToken);
                        submittedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-submitting exam session {SessionId}", baithi.Id);
                        
                        await _securityService.LogSecurityEventAsync("AUTO_SUBMIT_ERROR", 
                            $"Error auto-submitting session {baithi.Id}: {ex.Message}", 
                            baithi.IdTaiKhoan!.Value, "Medium", cancellationToken);
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
                if (answerDto.IdBaiThi <= 0)
                {
                    return new BaseResponseDto<bool>
                    {
                        Success = false,
                        Message = "ID bài thi không hợp lệ",
                        Data = false
                    };
                }

                if (answerDto.IdCauHoi <= 0)
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
                if (answerDto.IdLuaChonDaChon.HasValue && answerDto.IdLuaChonDaChon.Value <= 0)
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
        /// Auto-submit an expired exam
        /// </summary>
        private async Task AutoSubmitExpiredExam(Baithi baithi, int taikhoanId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Auto-submitting expired exam session {SessionId} for user {UserId}", baithi.Id, taikhoanId);

            // Calculate score based on saved answers
            var soCauDung = await _chitietRepository.CountCorrectAnswersAsync(baithi.Id);
            var tongDiem = CalculateScore(soCauDung, baithi.TongSoCau ?? 0);

            // Update exam status
            baithi.TrangThai = "Completed";
            baithi.ThoiGianNop = DateTime.Now;
            baithi.SoCauDung = soCauDung;
            baithi.TongDiem = tongDiem;

            await _baithiRepository.UpdateAsync(baithi);

            await _securityService.LogSecurityEventAsync("EXAM_AUTO_SUBMITTED", 
                $"Exam session {baithi.Id} auto-submitted for user {taikhoanId} due to time expiry", 
                taikhoanId, "Info", cancellationToken);
        }

        /// <summary>
        /// Check if exam time has expired
        /// </summary>
        private bool IsExamExpired(Dethi? dethi, DateTime? sessionStartTime)
        {
            if (dethi?.ThoiGianLamBai == null || sessionStartTime == null)
                return false;

            var examDurationMinutes = dethi.ThoiGianLamBai.Value;
            var examEndTime = sessionStartTime.Value.AddMinutes(examDurationMinutes);
            
            return DateTime.Now > examEndTime;
        }

        /// <summary>
        /// Calculate exam score with business rules
        /// </summary>
        private double CalculateScore(int correctAnswers, int totalQuestions)
        {
            if (totalQuestions == 0) return 0;
            
            // Basic scoring: each correct answer = 1 point
            // Can be enhanced with weighted scoring based on question difficulty
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