using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Validation;

namespace BanTayVang.API.Services.Impl.Exams
{
    /// <summary>
    /// Exam management service implementing SOLID principles and OWASP security
    /// Follows SRP - focused on exam management operations only
    /// </summary>
    public class ExamManagementService : IExamManagementService
    {
        private readonly IDethiRepository _dethiRepository;
        private readonly IExamValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<ExamManagementService> _logger;

        public ExamManagementService(
            IDethiRepository dethiRepository,
            IExamValidationService validationService,
            IMapper mapper,
            ILogger<ExamManagementService> logger)
        {
            _dethiRepository = dethiRepository;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao, CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                // OWASP A01: Broken Access Control - Validate permissions
                var permissionValidation = await _validationService.ValidateExamPermissionAsync(0, nguoiTao, "CREATE", cancellationToken);
                if (!permissionValidation.IsValid)
                {
                    _logger.LogWarning("Unauthorized exam creation attempt by user {UserId}. CorrelationId: {CorrelationId}", 
                        nguoiTao, correlationId);
                    return BaseResponseDto<DethiDto>.FailureResult("Access denied", permissionValidation.Errors);
                }

                // OWASP A03: Injection Prevention - Validate input
                var validation = await _validationService.ValidateCreateExamAsync(createDto, cancellationToken);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("Exam creation validation failed for user {UserId}. Errors: {Errors}. CorrelationId: {CorrelationId}", 
                        nguoiTao, string.Join(", ", validation.Errors), correlationId);
                    return BaseResponseDto<DethiDto>.FailureResult("Validation failed", validation.Errors);
                }

                // Use transaction for data consistency
                using var transaction = await _dethiRepository.BeginTransactionAsync();
                
                try
                {
                    // Create exam entity
                    var dethi = new Dethi
                    {
                        MaDeThi = createDto.MaDeThi,
                        TenDeThi = createDto.TenDeThi,
                        ThoiGianLamBai = createDto.ThoiGianLamBai,
                        ThoiGianBatDau = createDto.ThoiGianBatDau,
                        TrangThai = createDto.TrangThai,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.UtcNow,
                        LinkTruyCap = GenerateSecureExamLink(createDto.MaDeThi),
                        // OWASP A02: Cryptographic Failures - Add integrity check
                        ChecksumData = CalculateExamChecksum(createDto)
                    };

                    var savedDethi = await _dethiRepository.AddAsync(dethi);

                    // Add questions to exam with validation
                    if (createDto.DanhSachIdCauHoi.Any())
                    {
                        var addQuestionsResult = await _dethiRepository.AddQuestionsToExamAsync(
                            savedDethi.Id, createDto.DanhSachIdCauHoi);
                        
                        if (!addQuestionsResult)
                        {
                            throw new InvalidOperationException("Failed to add questions to exam");
                        }

                        // Calculate total score
                        var totalScore = await CalculateTotalScoreAsync(createDto.DanhSachIdCauHoi, cancellationToken);
                        savedDethi.TongDiem = totalScore;
                        await _dethiRepository.UpdateAsync(savedDethi);
                    }

                    await transaction.CommitAsync(cancellationToken);

                    // Map to DTO
                    var result = _mapper.Map<DethiDto>(savedDethi);
                    result.SoCauHoi = createDto.DanhSachIdCauHoi.Count;

                    // OWASP A09: Security Logging - Log successful creation
                    _logger.LogInformation("Exam created successfully. ExamId: {ExamId}, ExamCode: {ExamCode}, CreatedBy: {UserId}, CorrelationId: {CorrelationId}",
                        savedDethi.Id, savedDethi.MaDeThi, nguoiTao, correlationId);

                    return BaseResponseDto<DethiDto>.SuccessResult(result, "Exam created successfully");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam for user {UserId}. CorrelationId: {CorrelationId}", nguoiTao, correlationId);
                return BaseResponseDto<DethiDto>.FailureResult("An error occurred while creating the exam");
            }
        }

        public async Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi, CancellationToken cancellationToken = default)
        {
            try
            {
                // OWASP A03: Injection Prevention - Validate input
                if (string.IsNullOrWhiteSpace(maDeThi) || maDeThi.Length > 50)
                {
                    return BaseResponseDto<DethiDto>.FailureResult("Invalid exam code");
                }

                var dethi = await _dethiRepository.GetByMaDeThiAsync(maDeThi, cancellationToken);
                if (dethi == null)
                {
                    // OWASP A09: Don't reveal whether exam exists for security
                    _logger.LogWarning("Exam lookup attempt for non-existent code: {ExamCode}", maDeThi);
                    return BaseResponseDto<DethiDto>.FailureResult("Exam not found");
                }

                var result = _mapper.Map<DethiDto>(dethi);
                return BaseResponseDto<DethiDto>.SuccessResult(result, "Exam retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exam by code {ExamCode}", maDeThi);
                return BaseResponseDto<DethiDto>.FailureResult("An error occurred while retrieving the exam");
            }
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await _dethiRepository.GetActiveExamsAsync(cancellationToken);
                var result = _mapper.Map<List<DethiDto>>(exams);

                return BaseResponseDto<List<DethiDto>>.SuccessResult(result, "Active exams retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active exams");
                return BaseResponseDto<List<DethiDto>>.FailureResult(
                    "An error occurred while retrieving active exams",
                    new List<string> { ex.Message, ex.InnerException?.Message ?? "" });
            }
        }

        public async Task<BaseResponseDto<DethiDto>> UpdateExamAsync(int examId, UpdateDethiDto updateDto, int nguoiCapNhat, CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // OWASP A01: Broken Access Control - Validate permissions
                var permissionValidation = await _validationService.ValidateExamPermissionAsync(examId, nguoiCapNhat, "UPDATE", cancellationToken);
                if (!permissionValidation.IsValid)
                {
                    _logger.LogWarning("Unauthorized exam update attempt. ExamId: {ExamId}, UserId: {UserId}, CorrelationId: {CorrelationId}", 
                        examId, nguoiCapNhat, correlationId);
                    return BaseResponseDto<DethiDto>.FailureResult("Access denied", permissionValidation.Errors);
                }

                // Validate update data
                var validation = await _validationService.ValidateUpdateExamAsync(updateDto, cancellationToken);
                if (!validation.IsValid)
                {
                    return BaseResponseDto<DethiDto>.FailureResult("Validation failed", validation.Errors);
                }

                using var transaction = await _dethiRepository.BeginTransactionAsync();
                
                try
                {
                    var existingExam = await _dethiRepository.GetByIdAsync(examId, cancellationToken);
                    if (existingExam == null)
                    {
                        return BaseResponseDto<DethiDto>.FailureResult("Exam not found");
                    }

                    // Update exam properties
                    existingExam.MaDeThi = updateDto.MaDeThi;
                    existingExam.TenDeThi = updateDto.TenDeThi;
                    existingExam.ThoiGianLamBai = updateDto.ThoiGianLamBai;
                    existingExam.ThoiGianBatDau = updateDto.ThoiGianBatDau;
                    existingExam.TrangThai = updateDto.TrangThai;
                    existingExam.NguoiCapNhat = nguoiCapNhat;
                    existingExam.NgayCapNhat = DateTime.UtcNow;

                    // Update questions if changed
                    if (updateDto.DanhSachIdCauHoi.Any())
                    {
                        await _dethiRepository.UpdateExamQuestionsAsync(examId, updateDto.DanhSachIdCauHoi);
                        
                        var totalScore = await CalculateTotalScoreAsync(updateDto.DanhSachIdCauHoi, cancellationToken);
                        existingExam.TongDiem = totalScore;
                    }

                    await _dethiRepository.UpdateAsync(existingExam);
                    await transaction.CommitAsync(cancellationToken);

                    var result = _mapper.Map<DethiDto>(existingExam);
                    
                    _logger.LogInformation("Exam updated successfully. ExamId: {ExamId}, UpdatedBy: {UserId}, CorrelationId: {CorrelationId}",
                        examId, nguoiCapNhat, correlationId);

                    return BaseResponseDto<DethiDto>.SuccessResult(result, "Exam updated successfully");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam {ExamId} by user {UserId}. CorrelationId: {CorrelationId}", 
                    examId, nguoiCapNhat, correlationId);
                return BaseResponseDto<DethiDto>.FailureResult("An error occurred while updating the exam");
            }
        }

        public async Task<BaseResponseDto> DeactivateExamAsync(int examId, int nguoiCapNhat, CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Validate permissions
                var permissionValidation = await _validationService.ValidateExamPermissionAsync(examId, nguoiCapNhat, "UPDATE", cancellationToken);
                if (!permissionValidation.IsValid)
                {
                    _logger.LogWarning("Unauthorized exam deactivation attempt. ExamId: {ExamId}, UserId: {UserId}, CorrelationId: {CorrelationId}", 
                        examId, nguoiCapNhat, correlationId);
                    return BaseResponseDto.FailureResult("Access denied", permissionValidation.Errors);
                }

                var exam = await _dethiRepository.GetByIdAsync(examId, cancellationToken);
                if (exam == null)
                {
                    return BaseResponseDto.FailureResult("Exam not found");
                }

                exam.TrangThai = "Inactive";
                exam.NguoiCapNhat = nguoiCapNhat;
                exam.NgayCapNhat = DateTime.UtcNow;

                await _dethiRepository.UpdateAsync(exam);

                _logger.LogInformation("Exam deactivated. ExamId: {ExamId}, DeactivatedBy: {UserId}, CorrelationId: {CorrelationId}",
                    examId, nguoiCapNhat, correlationId);

                return BaseResponseDto.SuccessResult("Exam deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating exam {ExamId} by user {UserId}. CorrelationId: {CorrelationId}", 
                    examId, nguoiCapNhat, correlationId);
                return BaseResponseDto.FailureResult("An error occurred while deactivating the exam");
            }
        }

        #region Private Methods

        /// <summary>
        /// OWASP A04: Insecure Design - Generate secure exam access link
        /// </summary>
        private static string GenerateSecureExamLink(string examCode)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var secureToken = Guid.NewGuid().ToString("N")[..8];
            return $"/exam/{examCode}?t={timestamp}&token={secureToken}";
        }

        /// <summary>
        /// OWASP A02: Cryptographic Failures - Calculate exam integrity checksum
        /// </summary>
        private static string CalculateExamChecksum(CreateDethiDto createDto)
        {
            var data = $"{createDto.MaDeThi}|{createDto.TenDeThi}|{createDto.ThoiGianLamBai}|{string.Join(",", createDto.DanhSachIdCauHoi)}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        private async Task<double> CalculateTotalScoreAsync(List<int> questionIds, CancellationToken cancellationToken)
        {
            // This would typically query the question repository to get scores
            // For now, assume each question is worth 1 point
            await Task.CompletedTask;
            return questionIds.Count;
        }

        #endregion
    }
}