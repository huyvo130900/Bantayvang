using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Exams;
using BanTayVang.API.Services.Interfaces.Validation;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl.Exams
{
    /// <summary>
    /// Exam management service - hỗ trợ tạo đề thi với random câu hỏi theo khoa/phòng
    /// </summary>
    public class ExamManagementService : IExamManagementService
    {
        private readonly IExamPaperRepository _examPaperRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IExamValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamManagementService> _logger;

        public ExamManagementService(
            IExamPaperRepository dethiRepository,
            IQuestionRepository cauhoiRepository,
            IExamValidationService validationService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamManagementService> logger)
        {
            _examPaperRepository = dethiRepository;
            _questionRepository = cauhoiRepository;
            _validationService = validationService;
            _mapper = mapper;
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<ExamPaperDto>> CreateExamAsync(CreateExamPaperDto createDto, int createdBy, CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                ExamCampaign? examCampaign = null;
                if (createDto.KyThiId.HasValue)
                {
                    examCampaign = await _context.Set<ExamCampaign>()
                        .Include(k => k.Department)
                        .FirstOrDefaultAsync(k => k.Id == createDto.KyThiId.Value, cancellationToken);
                    if (examCampaign != null)
                    {
                        createDto.ExamPaperName = examCampaign.CampaignName;
                        createDto.StartTime = examCampaign.StartTime;
                        createDto.Department = examCampaign.Department?.DepartmentName;

                        if (examCampaign.DurationMinutes.HasValue && examCampaign.DurationMinutes.Value > 0)
                        {
                            createDto.DurationMinutes = examCampaign.DurationMinutes.Value;
                        }
                        else 
                        {
                            createDto.DurationMinutes = 60; // Default if not specified
                        }
                    }
                }

                var permissionValidation = await _validationService.ValidateExamPermissionAsync(0, createdBy, "CREATE", cancellationToken);
                if (!permissionValidation.IsValid)
                {
                    _logger.LogWarning("Unauthorized exam creation attempt by user {UserId}", createdBy);
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Access denied", permissionValidation.Errors);
                }

                var validation = await _validationService.ValidateCreateExamAsync(createDto, cancellationToken);
                if (!validation.IsValid)
                {
                    var errorMessage = validation.Errors.Any() ? string.Join(", ", validation.Errors) : "Validation failed";
                    return BaseResponseDto<ExamPaperDto>.FailureResult(errorMessage, validation.Errors);
                }

                using var transaction = await _examPaperRepository.BeginTransactionAsync();

                try
                {
                    var examPaper = new ExamPaper
                    {
                        ExamPaperCode = createDto.ExamPaperCode,
                        ExamPaperName = createDto.ExamPaperName,
                        DurationMinutes = createDto.DurationMinutes ?? 60,
                        StartTime = createDto.StartTime,
                        Status = createDto.Status,
                        Department = createDto.Department,
                        MinPassQuestions = createDto.MinPassQuestions,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.UtcNow,
                        LinkTruyCap = GenerateSecureExamLink(createDto.ExamPaperCode),
                        ChecksumData = CalculateExamChecksum(createDto),
                        KyThiId = createDto.KyThiId
                    };

                    var savedDethi = await _examPaperRepository.AddAsync(examPaper);

                    // Xác định danh sách câu hỏi
                    List<int> questionIds = new();

                    if (createDto.DanhSachIdCauHoi != null && createDto.DanhSachIdCauHoi.Any())
                    {
                        questionIds = createDto.DanhSachIdCauHoi;
                    }
                    else if (!string.IsNullOrWhiteSpace(createDto.Department))
                    {
                        // Lấy TẤT CẢ câu hỏi của khoa từ ngân hàng
                        var allQuestionsRaw = await _questionRepository.GetByKhoaPhongAsync(createDto.Department);
                        var allQuestions = allQuestionsRaw
                            .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                            .Select(g => g.First())
                            .ToList();

                        // Lưu metadata: Department vào đề thi
                        // Câu hỏi sẽ KHÔNG lưu vào ExamPaperQuestion ngay - sẽ random khi thi sinh bắt đầu thi
                        // Thay vào đó ta lưu tất cả câu hỏi của khoa vào pool
                        var random = new Random();
                        var selectedQuestions = examCampaign != null && examCampaign.TotalQuestions.HasValue && examCampaign.TotalQuestions.Value < allQuestions.Count
                            ? allQuestions.OrderBy(_ => random.Next()).Take(examCampaign.TotalQuestions.Value).ToList()
                            : allQuestions;

                        questionIds = selectedQuestions.Select(q => q.Id).ToList();

                        // Lưu config vào ChecksumData
                        savedDethi.ChecksumData = $"KHOA:{createDto.Department}|SO_CAU:{(examCampaign != null && examCampaign.TotalQuestions.HasValue ? examCampaign.TotalQuestions.Value : allQuestions.Count)}|POOL:{allQuestions.Count}";
                        savedDethi.TotalScore = questionIds.Count;
                    }

                    if (examCampaign != null && examCampaign.TotalQuestions.HasValue && questionIds.Count != examCampaign.TotalQuestions.Value)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        var errorMsg = questionIds.Count < examCampaign.TotalQuestions.Value
                            ? $"Số lượng câu hỏi chưa đủ, còn thiếu {examCampaign.TotalQuestions.Value - questionIds.Count} câu hỏi"
                            : $"Số lượng câu hỏi vượt quá yêu cầu, thừa {questionIds.Count - examCampaign.TotalQuestions.Value} câu hỏi";
                        return BaseResponseDto<ExamPaperDto>.FailureResult(errorMsg);
                    }

                    if (questionIds.Any())
                    {
                        var addResult = await _examPaperRepository.AddQuestionsToExamAsync(savedDethi.Id, questionIds);
                        if (!addResult)
                            throw new InvalidOperationException("Failed to add questions to exam");

                        savedDethi.TotalScore = questionIds.Count;
                        await _examPaperRepository.UpdateAsync(savedDethi);
                    }

                    // Validate: MinPassQuestions cannot exceed total questions
                    if (createDto.MinPassQuestions.HasValue && createDto.MinPassQuestions.Value > questionIds.Count)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BaseResponseDto<ExamPaperDto>.FailureResult(
                            $"Số câu đúng tối thiểu ({createDto.MinPassQuestions.Value}) không được lớn hơn tổng số câu hỏi của đề thi ({questionIds.Count})");
                    }

                    await transaction.CommitAsync(cancellationToken);

                    var result = _mapper.Map<ExamPaperDto>(savedDethi);
                    result.TotalQuestions = questionIds.Count;

                    _logger.LogInformation("Exam created: {ExamId}, Code: {Code}, Questions: {Count}, Department: {Department}",
                        savedDethi.Id, savedDethi.ExamPaperCode, questionIds.Count, createDto.Department);

                    return BaseResponseDto<ExamPaperDto>.SuccessResult(result, "Tạo đề thi thành công");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam for user {UserId}", createdBy);
                return BaseResponseDto<ExamPaperDto>.FailureResult("Có lỗi xảy ra khi tạo đề thi");
            }
        }

        public async Task<BaseResponseDto<ExamPaperDto>> GetExamByCodeAsync(string examPaperCode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(examPaperCode) || examPaperCode.Length > 50)
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Invalid exam code");

                var examPaper = await _examPaperRepository.GetByMaDeThiAsync(examPaperCode, cancellationToken);
                if (examPaper == null)
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Exam not found");

                var result = _mapper.Map<ExamPaperDto>(examPaper);
                return BaseResponseDto<ExamPaperDto>.SuccessResult(result, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exam by code {ExamCode}", examPaperCode);
                return BaseResponseDto<ExamPaperDto>.FailureResult("An error occurred");
            }
        }

        public async Task<BaseResponseDto<List<ExamPaperDto>>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await _examPaperRepository.GetActiveExamsAsync(cancellationToken);
                var result = _mapper.Map<List<ExamPaperDto>>(exams);
                return BaseResponseDto<List<ExamPaperDto>>.SuccessResult(result, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active exams");
                return BaseResponseDto<List<ExamPaperDto>>.FailureResult("An error occurred", new List<string> { ex.Message });
            }
        }

        public async Task<BaseResponseDto<List<ExamPaperDto>>> GetAllExamsAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await _examPaperRepository.GetAllExamsAsync(status, cancellationToken);
                var dtos = exams.Select(e => _mapper.Map<ExamPaperDto>(e)).ToList();
                return new BaseResponseDto<List<ExamPaperDto>> { Success = true, Data = dtos };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto<List<ExamPaperDto>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamPaperDto>> UpdateExamAsync(int examId, UpdateExamPaperDto updateDto, int updatedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                ExamCampaign? examCampaign = null;
                if (updateDto.KyThiId.HasValue)
                {
                    examCampaign = await _context.Set<ExamCampaign>()
                        .Include(k => k.Department)
                        .FirstOrDefaultAsync(k => k.Id == updateDto.KyThiId.Value, cancellationToken);
                    if (examCampaign != null)
                    {
                        updateDto.ExamPaperName = examCampaign.CampaignName;
                        updateDto.StartTime = examCampaign.StartTime;

                        if (examCampaign.DurationMinutes.HasValue && examCampaign.DurationMinutes.Value > 0)
                        {
                            updateDto.DurationMinutes = examCampaign.DurationMinutes.Value;
                        }
                    }
                }

                var permissionValidation = await _validationService.ValidateExamPermissionAsync(examId, updatedBy, "UPDATE", cancellationToken);
                if (!permissionValidation.IsValid)
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Access denied", permissionValidation.Errors);

                using var transaction = await _examPaperRepository.BeginTransactionAsync();

                try
                {
                    var existingExam = await _examPaperRepository.GetByIdAsync(examId, cancellationToken);
                    if (existingExam == null)
                        return BaseResponseDto<ExamPaperDto>.FailureResult("Exam not found");

                    existingExam.ExamPaperCode = updateDto.ExamPaperCode;
                    existingExam.ExamPaperName = updateDto.ExamPaperName;
                    existingExam.DurationMinutes = updateDto.DurationMinutes ?? existingExam.DurationMinutes ?? 60;
                    existingExam.StartTime = updateDto.StartTime;
                    existingExam.Status = updateDto.Status;
                    existingExam.KyThiId = updateDto.KyThiId;
                    if (examCampaign != null)
                    {
                        existingExam.Department = examCampaign.Department?.DepartmentName;
                    }
                    existingExam.UpdatedBy = updatedBy;
                    existingExam.UpdatedAt = DateTime.UtcNow;

                    if (updateDto.DanhSachIdCauHoi.Any())
                    {
                        if (examCampaign != null && examCampaign.TotalQuestions.HasValue && updateDto.DanhSachIdCauHoi.Count != examCampaign.TotalQuestions.Value)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            var errorMsg = updateDto.DanhSachIdCauHoi.Count < examCampaign.TotalQuestions.Value
                                ? $"Số lượng câu hỏi chưa đủ, còn thiếu {examCampaign.TotalQuestions.Value - updateDto.DanhSachIdCauHoi.Count} câu hỏi"
                                : $"Số lượng câu hỏi vượt quá yêu cầu, thừa {updateDto.DanhSachIdCauHoi.Count - examCampaign.TotalQuestions.Value} câu hỏi";
                            return BaseResponseDto<ExamPaperDto>.FailureResult(errorMsg);
                        }

                        await _examPaperRepository.UpdateExamQuestionsAsync(examId, updateDto.DanhSachIdCauHoi);
                        existingExam.TotalScore = updateDto.DanhSachIdCauHoi.Count;
                    }

                    await _examPaperRepository.UpdateAsync(existingExam);
                    await transaction.CommitAsync(cancellationToken);

                    var result = _mapper.Map<ExamPaperDto>(existingExam);
                    return BaseResponseDto<ExamPaperDto>.SuccessResult(result, "Cập nhật thành công");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam {ExamId}", examId);
                return BaseResponseDto<ExamPaperDto>.FailureResult("Có lỗi xảy ra");
            }
        }

        public async Task<BaseResponseDto<ExamPaperDto>> UpdateExamStatusAsync(int examId, string status, int updatedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status) || !System.Text.RegularExpressions.Regex.IsMatch(status, "^(Draft|Active|Inactive|Archived)$"))
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Invalid status value");

                var exam = await _examPaperRepository.GetByIdAsync(examId, cancellationToken);
                if (exam == null)
                    return BaseResponseDto<ExamPaperDto>.FailureResult("Exam not found");

                exam.Status = status;
                exam.UpdatedBy = updatedBy;
                exam.UpdatedAt = DateTime.UtcNow;

                await _examPaperRepository.UpdateAsync(exam);

                var result = _mapper.Map<ExamPaperDto>(exam);
                return BaseResponseDto<ExamPaperDto>.SuccessResult(result, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam status {ExamId}", examId);
                return BaseResponseDto<ExamPaperDto>.FailureResult("Có lỗi xảy ra");
            }
        }

        public async Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _examPaperRepository.DeleteAsync(examId);
                if (!deleted)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy đề thi" };
                return new BaseResponseDto { Success = true, Message = "Đã xóa đề thi" };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeactivateExamAsync(int examId, int updatedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                var exam = await _examPaperRepository.GetByIdAsync(examId, cancellationToken);
                if (exam == null)
                    return BaseResponseDto.FailureResult("Exam not found");

                exam.Status = "Inactive";
                exam.UpdatedBy = updatedBy;
                exam.UpdatedAt = DateTime.UtcNow;
                await _examPaperRepository.UpdateAsync(exam);
                return BaseResponseDto.SuccessResult("Exam deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating exam {ExamId}", examId);
                return BaseResponseDto.FailureResult("Có lỗi xảy ra");
            }
        }

        #region Private Methods

        private static string GenerateSecureExamLink(string examCode)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var secureToken = Guid.NewGuid().ToString("N")[..8];
            return $"/exam/{examCode}?t={timestamp}&token={secureToken}";
        }

        private static string CalculateExamChecksum(CreateExamPaperDto createDto)
        {
            var data = $"{createDto.ExamPaperCode}|{createDto.ExamPaperName}|{createDto.DurationMinutes ?? 60}|{createDto.Department}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}
