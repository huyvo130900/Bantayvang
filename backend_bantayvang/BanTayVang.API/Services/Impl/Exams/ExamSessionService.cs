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
        private readonly IDethiRepository _dethiRepository;
        private readonly IBaithiRepository _baithiRepository;
        private readonly IChitietlambaiRepository _chitietRepository;
        private readonly IExamValidationService _validationService;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamSessionService> _logger;

        public ExamSessionService(
            IDethiRepository dethiRepository,
            IBaithiRepository baithiRepository,
            IChitietlambaiRepository chitietRepository,
            IExamValidationService validationService,
            IExamSecurityService securityService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamSessionService> logger)
        {
            _dethiRepository = dethiRepository ?? throw new ArgumentNullException(nameof(dethiRepository));
            _baithiRepository = baithiRepository ?? throw new ArgumentNullException(nameof(baithiRepository));
            _chitietRepository = chitietRepository ?? throw new ArgumentNullException(nameof(chitietRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BaseResponseDto<BaithiDto>> StartExamAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting exam session for user {UserId} with exam code {ExamCode}, KyThiId {KyThiId}", taikhoanId, startDto.MaDeThi, startDto.KyThiId);

                // OWASP A03: Injection - Input validation
                var validationResult = await _validationService.ValidateStartExamAsync(startDto, taikhoanId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_START_VALIDATION_FAILED",
                        $"User {taikhoanId} failed validation for exam {startDto.MaDeThi}",
                        taikhoanId, "Medium", cancellationToken);
                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = validationResult.Message,
                        Errors = validationResult.Errors
                    };
                }

                // Resolve exam paper (Dethi)
                // Rule: Thí sinh làm các đề khác nhau khi thi lại. Làm hết các đề mới lặp lại.
                Dethi? dethi = null;
                if (startDto.KyThiId.HasValue)
                {
                    dethi = await _dethiRepository.ResolveExamForCandidateAsync(startDto.KyThiId.Value, taikhoanId, cancellationToken);
                }

                // Fallback to MaDeThi if not resolved via KyThiId
                if (dethi == null)
                {
                    if (string.IsNullOrEmpty(startDto.MaDeThi))
                    {
                        return new BaseResponseDto<BaithiDto> { Success = false, Message = "Kỳ thi chưa có đề thi" };
                    }
                    dethi = await _dethiRepository.GetByMaDeThiAsync(startDto.MaDeThi);
                }

                if (dethi == null)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_NOT_FOUND",
                        $"User {taikhoanId} attempted to access non-existent exam {startDto.MaDeThi}",
                        taikhoanId, "Medium", cancellationToken);

                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy đề thi"
                    };
                }

                // OWASP A01: Broken Access Control - Time-based access control
                if (dethi.ThoiGianBatDau > DateTime.Now)
                {
                    await _securityService.LogSecurityEventAsync("EXAM_EARLY_ACCESS_ATTEMPT",
                        $"User {taikhoanId} attempted early access to exam {dethi.MaDeThi}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = "Chưa đến thời gian thi"
                    };
                }

                // Check for existing IN-PROGRESS session (resume it)
                var existingBaithi = await _baithiRepository.GetActiveExamSessionAsync(taikhoanId, dethi.Id);

                Baithi baithi;
                if (existingBaithi != null && existingBaithi.TrangThai == "InProgress")
                {
                    // Resume existing in-progress session
                    baithi = existingBaithi;
                    _logger.LogInformation("Resuming existing exam session {SessionId} for user {UserId}", baithi.Id, taikhoanId);
                }
                else
                {
                    // Create new session
                    var dethiWithQuestions = await _dethiRepository.GetWithQuestionsAsync(dethi.Id);

                    // Lấy toàn bộ câu hỏi, loại trùng lặp theo IdCauHoi
                    var allQuestions = (dethiWithQuestions?.DethiCauhois ?? new List<DethiCauhoi>())
                        .Where(dc => dc.IdCauHoi.HasValue && dc.IdCauHoiNavigation != null)
                        .GroupBy(dc => dc.IdCauHoi!.Value)
                        .Select(g => g.First())
                        .ToList();

                    // Đọc giới hạn số câu từ ChecksumData (nếu có)
                    int soCauLimit = allQuestions.Count;
                    if (!string.IsNullOrEmpty(dethi.ChecksumData) && dethi.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = dethi.ChecksumData.Split('|');
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

                    baithi = new Baithi
                    {
                        IdTaiKhoan = taikhoanId,
                        IdDeThi = dethi.Id,
                        MaDeThi = dethi.MaDeThi,
                        IdKyThi = startDto.KyThiId ?? dethi.KyThiId, // Link KyThiId to the exam session
                        TrangThai = "InProgress",
                        TongSoCau = selectedQuestions.Count,
                        TongSoCanhBao = 0,
                        ThoiGianBatDau = DateTime.Now
                    };

                    baithi = await _baithiRepository.AddAsync(baithi);

                    // Lưu thứ tự câu hỏi đã chọn vào Chitietlambai (placeholder, chưa có đáp án)
                    for (int i = 0; i < selectedQuestions.Count; i++)
                    {
                        var placeholder = new Chitietlambai
                        {
                            IdBaiThi = baithi.Id,
                            IdCauHoi = selectedQuestions[i].IdCauHoi,
                            IdLuaChonDaChon = null,
                            CauTraLoiTuLuan = null,
                            ThoiGianTraLoi = null,
                            DaLuu = false,
                            DiemDatDuoc = null
                        };
                        await _chitietRepository.AddAsync(placeholder);
                    }

                    await _securityService.LogSecurityEventAsync("EXAM_SESSION_STARTED",
                        $"User {taikhoanId} started exam session {baithi.Id} for exam {dethi.MaDeThi} with {selectedQuestions.Count} questions",
                        taikhoanId, "Info", cancellationToken);

                    _logger.LogInformation("Created new exam session {SessionId} for user {UserId} with {Count} unique questions", baithi.Id, taikhoanId, selectedQuestions.Count);
                }

                var result = _mapper.Map<BaithiDto>(baithi);
                result.TenDeThi = dethi.TenDeThi;
                result.ThoiGianLamBai = dethi.ThoiGianLamBai;
                result.ThoiGianBatDau = dethi.ThoiGianBatDau;

                // Calculate remaining time with security validation
                result.ThoiGianConLai = CalculateRemainingTime(dethi, baithi.ThoiGianBatDau);

                return new BaseResponseDto<BaithiDto>
                {
                    Success = true,
                    Message = "Bắt đầu làm bài thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting exam session for user {UserId} with exam code {ExamCode}", taikhoanId, startDto.MaDeThi);

                await _securityService.LogSecurityEventAsync("EXAM_START_ERROR",
                    $"System error during exam start for user {taikhoanId}: {ex.Message}",
                    taikhoanId, "High", cancellationToken);

                return new BaseResponseDto<BaithiDto>
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
                var baithi = await _baithiRepository.GetByIdAsync(baithiId);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
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

                if (baithi.TrangThai != "InProgress")
                {
                    return new BaseResponseDto<List<ExamQuestionDto>>
                    {
                        Success = false,
                        Message = "Bài thi không ở trạng thái làm bài"
                    };
                }

                var dethi = await _dethiRepository.GetWithQuestionsAsync(baithi.IdDeThi!.Value);
                if (dethi == null)
                {
                    return new BaseResponseDto<List<ExamQuestionDto>>
                    {
                        Success = false,
                        Message = "Không tìm thấy đề thi"
                    };
                }

                var questions = new List<ExamQuestionDto>();

                // Đọc danh sách câu hỏi từ Chitietlambai đã được lưu lúc StartExam
                // → thứ tự cố định, không shuffle lại, không bị trùng lặp
                var savedChitiets = await _chitietRepository.GetByBaiThiAsync(baithiId);

                // Distinct theo IdCauHoi để phòng trường hợp dữ liệu cũ có bản ghi trùng
                var orderedChitiets = savedChitiets
                    .Where(c => c.IdCauHoi.HasValue)
                    .GroupBy(c => c.IdCauHoi!.Value)
                    .Select(g => g.OrderBy(c => c.Id).First()) // lấy bản ghi cũ nhất
                    .OrderBy(c => c.Id)                         // giữ thứ tự insert lúc Start
                    .ToList();

                // Nếu session cũ (chưa có placeholder), fallback về logic cũ nhưng dùng seed cố định
                if (!orderedChitiets.Any())
                {
                    var random = new Random(baithiId);
                    int? soCauLimit = null;
                    if (!string.IsNullOrEmpty(dethi.ChecksumData) && dethi.ChecksumData.Contains("SO_CAU:"))
                    {
                        try
                        {
                            var parts = dethi.ChecksumData.Split('|');
                            var soCauPart = parts.FirstOrDefault(p => p.StartsWith("SO_CAU:"));
                            if (soCauPart != null && int.TryParse(soCauPart.Replace("SO_CAU:", ""), out int soCauConfig))
                                soCauLimit = soCauConfig;
                        }
                        catch { /* ignore */ }
                    }

                    var allDethiCauhois = dethi.DethiCauhois
                        .Where(dc => dc.IdCauHoi.HasValue && dc.IdCauHoiNavigation != null)
                        .GroupBy(dc => dc.IdCauHoi!.Value)
                        .Select(g => g.First())
                        .ToList();

                    var shuffled = allDethiCauhois
                        .OrderBy(_ => random.Next())
                        .Take(soCauLimit.HasValue ? Math.Min(soCauLimit.Value, allDethiCauhois.Count) : allDethiCauhois.Count)
                        .ToList();

                    int thuTuFallback = 1;
                    foreach (var dethiCauhoi in shuffled)
                    {
                        var cauhoi = dethiCauhoi.IdCauHoiNavigation!;
                        var choiceRandom = new Random(baithiId * 1000 + cauhoi.Id);
                        var shuffledChoices = cauhoi.Luachons
                            .OrderBy(_ => choiceRandom.Next())
                            .Select((l, idx) => new ExamChoiceDto { Id = l.Id, NoiDung = SanitizeHtmlContent(l.NoiDung), ThuTu = idx + 1 })
                            .ToList();

                        questions.Add(new ExamQuestionDto
                        {
                            Id = cauhoi.Id,
                            NoiDung = SanitizeHtmlContent(cauhoi.NoiDung),
                            HinhAnh = cauhoi.HinhAnh,
                            ThuTuCau = thuTuFallback++,
                            DanhSachLuaChon = shuffledChoices,
                            ChoPhepChonNhieu = cauhoi.Luachons.Count(l => l.LaDapAnDung == true) > 1
                        });
                    }
                }
                else
                {
                    // Nhóm chi tiết làm bài theo IdCauHoi để lấy toàn bộ đáp án đã chọn (hỗ trợ chọn nhiều)
                    var chitietsByQuestion = savedChitiets
                        .Where(c => c.IdCauHoi.HasValue)
                        .GroupBy(c => c.IdCauHoi!.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var cauhoiIds = orderedChitiets.Select(c => c.IdCauHoi!.Value).ToList();
                    var dethiCauhoisMap = dethi.DethiCauhois
                        .Where(dc => dc.IdCauHoi.HasValue && cauhoiIds.Contains(dc.IdCauHoi.Value))
                        .GroupBy(dc => dc.IdCauHoi!.Value)
                        .ToDictionary(g => g.Key, g => g.First().IdCauHoiNavigation);

                    int thuTu = 1;
                    foreach (var ct in orderedChitiets)
                    {
                        var cauHoiId = ct.IdCauHoi!.Value;
                        if (!dethiCauhoisMap.TryGetValue(cauHoiId, out var cauhoi) || cauhoi == null)
                            continue;

                        // Lấy tất cả lựa chọn đã chọn cho câu hỏi này
                        var questionChitiets = chitietsByQuestion.ContainsKey(cauHoiId) 
                            ? chitietsByQuestion[cauHoiId] 
                            : new List<Chitietlambai> { ct };

                        var selectedChoiceIds = questionChitiets
                            .Where(c => c.IdLuaChonDaChon.HasValue)
                            .Select(c => c.IdLuaChonDaChon!.Value)
                            .ToList();

                        // Shuffle đáp án dùng seed cố định theo session + câu hỏi
                        var choiceRandom = new Random(baithiId * 1000 + cauhoi.Id);
                        var shuffledChoices = cauhoi.Luachons
                            .OrderBy(_ => choiceRandom.Next())
                            .Select((l, idx) => new ExamChoiceDto
                            {
                                Id = l.Id,
                                NoiDung = SanitizeHtmlContent(l.NoiDung),
                                ThuTu = idx + 1
                            })
                            .ToList();

                        var questionDto = new ExamQuestionDto
                        {
                            Id = cauhoi.Id,
                            NoiDung = SanitizeHtmlContent(cauhoi.NoiDung),
                            HinhAnh = cauhoi.HinhAnh,
                            ThuTuCau = thuTu++,
                            DanhSachLuaChon = shuffledChoices,
                            IdLuaChonDaChon = selectedChoiceIds.Count > 0 ? (int?)selectedChoiceIds[0] : null,
                            IdLuaChonDaChonList = selectedChoiceIds,
                            CauTraLoiTuLuan = SanitizeHtmlContent(questionChitiets.First().CauTraLoiTuLuan),
                            DaLuu = questionChitiets.Any(c => c.DaLuu ?? false),
                            ChoPhepChonNhieu = cauhoi.Luachons.Count(l => l.LaDapAnDung == true) > 1
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

        public async Task<BaseResponseDto<BaithiDto>> GetExamProgressAsync(int baithiId, int taikhoanId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting exam progress for session {SessionId} and user {UserId}", baithiId, taikhoanId);

                // OWASP A01: Broken Access Control - Verify ownership
                var baithi = await _baithiRepository.GetWithDetailsAsync(baithiId);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
                {
                    await _securityService.LogSecurityEventAsync("UNAUTHORIZED_PROGRESS_ACCESS",
                        $"User {taikhoanId} attempted unauthorized access to progress of session {baithiId}",
                        taikhoanId, "High", cancellationToken);

                    return new BaseResponseDto<BaithiDto>
                    {
                        Success = false,
                        Message = "Không có quyền truy cập bài thi này"
                    };
                }

                var result = _mapper.Map<BaithiDto>(baithi);
                result.TenDeThi = baithi.IdDeThiNavigation?.TenDeThi;
                result.ThoiGianLamBai = baithi.IdDeThiNavigation?.ThoiGianLamBai;
                result.ThoiGianBatDau = baithi.IdDeThiNavigation?.ThoiGianBatDau;

                // Calculate remaining time
                result.ThoiGianConLai = CalculateRemainingTime(baithi.IdDeThiNavigation, baithi.ThoiGianBatDau);

                return new BaseResponseDto<BaithiDto>
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

                return new BaseResponseDto<BaithiDto>
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
                var baithi = await _baithiRepository.GetByIdAsync(baithiId);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
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

                if (baithi.TrangThai != "InProgress")
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không thể tạm dừng bài thi ở trạng thái hiện tại"
                    };
                }

                baithi.TrangThai = "Paused";
                await _baithiRepository.UpdateAsync(baithi);

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
                var baithi = await _baithiRepository.GetByIdAsync(baithiId);
                if (baithi == null || baithi.IdTaiKhoan != taikhoanId)
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

                if (baithi.TrangThai != "Paused")
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Bài thi không ở trạng thái tạm dừng"
                    };
                }

                baithi.TrangThai = "InProgress";
                await _baithiRepository.UpdateAsync(baithi);

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
        private int CalculateRemainingTime(Dethi? dethi, DateTime? sessionStartTime)
        {
            if (dethi?.ThoiGianBatDau == null || dethi.ThoiGianLamBai == null || sessionStartTime == null)
            {
                return 0;
            }

            var thoiGianDaLam = DateTime.Now - sessionStartTime.Value;
            var thoiGianConLai = (dethi.ThoiGianLamBai.Value * 60) - (int)thoiGianDaLam.TotalSeconds;
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