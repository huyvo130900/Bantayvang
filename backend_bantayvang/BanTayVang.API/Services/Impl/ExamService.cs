using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Security;
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
        private readonly ICanhbaogianlanRepository _canhbaoRepository;
        private readonly ILogger<ExamService> _logger;

        public ExamService(
            IExamManagementService managementService,
            IExamSessionService sessionService,
            IExamSubmissionService submissionService,
            IExamSecurityService securityService,
            ICanhbaogianlanRepository canhbaoRepository,
            ILogger<ExamService> logger)
        {
            _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _canhbaoRepository = canhbaoRepository ?? throw new ArgumentNullException(nameof(canhbaoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Exam Management Operations (Delegated to IExamManagementService)

        public async Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao)
        {
            return await _managementService.CreateExamAsync(createDto, nguoiTao);
        }

        public async Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi)
        {
            return await _managementService.GetExamByCodeAsync(maDeThi);
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync()
        {
            return await _managementService.GetActiveExamsAsync();
        }

        #endregion

        #region Exam Session Operations (Delegated to IExamSessionService)

        public async Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId)
        {
            return await _sessionService.StartExamAsync(startDto, taikhoanId);
        }

        public async Task<BaseResponseDto<List<ExamQuestionDto>>> GetExamQuestionsAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamQuestionsAsync(baithiId, taikhoanId);
        }

        public async Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId)
        {
            return await _sessionService.GetExamProgressAsync(baithiId, taikhoanId);
        }

        #endregion

        #region Exam Submission Operations (Delegated to IExamSubmissionService)

        public async Task<BaseResponseDto> SaveAnswerAsync(SubmitAnswerDto answerDto, int taikhoanId)
        {
            return await _submissionService.SaveAnswerAsync(answerDto, taikhoanId);
        }

        public async Task<BaseResponseDto<BaithiDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId)
        {
            return await _submissionService.SubmitExamAsync(submitDto, taikhoanId);
        }

        public async Task<BaseResponseDto> AutoSubmitExpiredExamsAsync()
        {
            return await _submissionService.AutoSubmitExpiredExamsAsync();
        }

        #endregion

        #region Security Operations (Enhanced with OWASP compliance)

        public async Task<BaseResponseDto> LogSuspiciousActivityAsync(int baithiId, string loaiCanhBao, string moTa)
        {
            try
            {
                _logger.LogWarning("Suspicious activity detected - Session: {SessionId}, Type: {Type}, Description: {Description}", 
                    baithiId, loaiCanhBao, moTa);

                // OWASP A09: Security Logging - Enhanced security event logging
                var canhbao = new Canhbaogianlan
                {
                    IdBaiThi = baithiId,
                    LoaiCanhBao = loaiCanhBao,
                    MoTa = SanitizeInput(moTa), // OWASP A03: Injection prevention
                    ThoiGian = DateTime.Now
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
    }
}