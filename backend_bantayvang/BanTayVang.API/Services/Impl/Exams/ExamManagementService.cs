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
        private readonly IDethiRepository _dethiRepository;
        private readonly ICauhoiRepository _cauhoiRepository;
        private readonly IExamValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamManagementService> _logger;

        public ExamManagementService(
            IDethiRepository dethiRepository,
            ICauhoiRepository cauhoiRepository,
            IExamValidationService validationService,
            IMapper mapper,
            BanTayVangDbContext context,
            ILogger<ExamManagementService> logger)
        {
            _dethiRepository = dethiRepository;
            _cauhoiRepository = cauhoiRepository;
            _validationService = validationService;
            _mapper = mapper;
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<DethiDto>> CreateExamAsync(CreateDethiDto createDto, int nguoiTao, CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                KyThi? kyThi = null;
                if (createDto.KyThiId.HasValue)
                {
                    kyThi = await _context.Set<KyThi>()
                        .Include(k => k.KhoaPhong)
                        .FirstOrDefaultAsync(k => k.Id == createDto.KyThiId.Value, cancellationToken);
                    if (kyThi != null)
                    {
                        createDto.TenDeThi = kyThi.TenKyThi;
                        createDto.ThoiGianBatDau = kyThi.ThoiGianBatDau;
                        createDto.TrangThai = kyThi.TrangThai;
                        createDto.KhoaPhong = kyThi.KhoaPhong?.TenKhoa;

                        if (kyThi.ThoiGianBatDau.HasValue && kyThi.ThoiGianKetThuc.HasValue)
                        {
                            var diff = kyThi.ThoiGianKetThuc.Value - kyThi.ThoiGianBatDau.Value;
                            var duration = (int)diff.TotalMinutes;
                            if (duration > 0)
                            {
                                createDto.ThoiGianLamBai = duration;
                            }
                        }
                    }
                }

                var permissionValidation = await _validationService.ValidateExamPermissionAsync(0, nguoiTao, "CREATE", cancellationToken);
                if (!permissionValidation.IsValid)
                {
                    _logger.LogWarning("Unauthorized exam creation attempt by user {UserId}", nguoiTao);
                    return BaseResponseDto<DethiDto>.FailureResult("Access denied", permissionValidation.Errors);
                }

                var validation = await _validationService.ValidateCreateExamAsync(createDto, cancellationToken);
                if (!validation.IsValid)
                {
                    var errorMessage = validation.Errors.Any() ? string.Join(", ", validation.Errors) : "Validation failed";
                    return BaseResponseDto<DethiDto>.FailureResult(errorMessage, validation.Errors);
                }

                using var transaction = await _dethiRepository.BeginTransactionAsync();

                try
                {
                    var dethi = new Dethi
                    {
                        MaDeThi = createDto.MaDeThi,
                        TenDeThi = createDto.TenDeThi,
                        ThoiGianLamBai = createDto.ThoiGianLamBai ?? 60,
                        ThoiGianBatDau = createDto.ThoiGianBatDau,
                        TrangThai = createDto.TrangThai,
                        KhoaPhong = createDto.KhoaPhong,
                        SoCauDungToiThieu = createDto.SoCauDungToiThieu,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.UtcNow,
                        LinkTruyCap = GenerateSecureExamLink(createDto.MaDeThi),
                        ChecksumData = CalculateExamChecksum(createDto),
                        KyThiId = createDto.KyThiId
                    };

                    var savedDethi = await _dethiRepository.AddAsync(dethi);

                    // Xác định danh sách câu hỏi
                    List<int> questionIds = new();

                    if (createDto.DanhSachIdCauHoi != null && createDto.DanhSachIdCauHoi.Any())
                    {
                        questionIds = createDto.DanhSachIdCauHoi;
                    }
                    else if (!string.IsNullOrWhiteSpace(createDto.KhoaPhong))
                    {
                        // Lấy TẤT CẢ câu hỏi của khoa từ ngân hàng
                        var allQuestions = await _cauhoiRepository.GetByKhoaPhongAsync(createDto.KhoaPhong);

                        // Lưu metadata: KhoaPhong vào đề thi
                        // Câu hỏi sẽ KHÔNG lưu vào DethiCauhoi ngay - sẽ random khi thi sinh bắt đầu thi
                        // Thay vào đó ta lưu tất cả câu hỏi của khoa vào pool
                        var random = new Random();
                        var selectedQuestions = kyThi != null && kyThi.TongSoCauHoi.HasValue && kyThi.TongSoCauHoi.Value < allQuestions.Count
                            ? allQuestions.OrderBy(_ => random.Next()).Take(kyThi.TongSoCauHoi.Value).ToList()
                            : allQuestions;

                        questionIds = selectedQuestions.Select(q => q.Id).ToList();

                        // Lưu config vào ChecksumData
                        savedDethi.ChecksumData = $"KHOA:{createDto.KhoaPhong}|SO_CAU:{(kyThi != null && kyThi.TongSoCauHoi.HasValue ? kyThi.TongSoCauHoi.Value : allQuestions.Count)}|POOL:{allQuestions.Count}";
                        savedDethi.TongDiem = questionIds.Count;
                    }

                    if (kyThi != null && kyThi.TongSoCauHoi.HasValue && questionIds.Count != kyThi.TongSoCauHoi.Value)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        var errorMsg = questionIds.Count < kyThi.TongSoCauHoi.Value
                            ? $"Số lượng câu hỏi chưa đủ, còn thiếu {kyThi.TongSoCauHoi.Value - questionIds.Count} câu hỏi"
                            : $"Số lượng câu hỏi vượt quá yêu cầu, thừa {questionIds.Count - kyThi.TongSoCauHoi.Value} câu hỏi";
                        return BaseResponseDto<DethiDto>.FailureResult(errorMsg);
                    }

                    if (questionIds.Any())
                    {
                        var addResult = await _dethiRepository.AddQuestionsToExamAsync(savedDethi.Id, questionIds);
                        if (!addResult)
                            throw new InvalidOperationException("Failed to add questions to exam");

                        savedDethi.TongDiem = questionIds.Count;
                        await _dethiRepository.UpdateAsync(savedDethi);
                    }

                    // Validate: SoCauDungToiThieu cannot exceed total questions
                    if (createDto.SoCauDungToiThieu.HasValue && createDto.SoCauDungToiThieu.Value > questionIds.Count)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BaseResponseDto<DethiDto>.FailureResult(
                            $"Số câu đúng tối thiểu ({createDto.SoCauDungToiThieu.Value}) không được lớn hơn tổng số câu hỏi của đề thi ({questionIds.Count})");
                    }

                    await transaction.CommitAsync(cancellationToken);

                    var result = _mapper.Map<DethiDto>(savedDethi);
                    result.SoCauHoi = questionIds.Count;

                    _logger.LogInformation("Exam created: {ExamId}, Code: {Code}, Questions: {Count}, KhoaPhong: {KhoaPhong}",
                        savedDethi.Id, savedDethi.MaDeThi, questionIds.Count, createDto.KhoaPhong);

                    return BaseResponseDto<DethiDto>.SuccessResult(result, "Tạo đề thi thành công");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam for user {UserId}", nguoiTao);
                return BaseResponseDto<DethiDto>.FailureResult("Có lỗi xảy ra khi tạo đề thi");
            }
        }

        public async Task<BaseResponseDto<DethiDto>> GetExamByCodeAsync(string maDeThi, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maDeThi) || maDeThi.Length > 50)
                    return BaseResponseDto<DethiDto>.FailureResult("Invalid exam code");

                var dethi = await _dethiRepository.GetByMaDeThiAsync(maDeThi, cancellationToken);
                if (dethi == null)
                    return BaseResponseDto<DethiDto>.FailureResult("Exam not found");

                var result = _mapper.Map<DethiDto>(dethi);
                return BaseResponseDto<DethiDto>.SuccessResult(result, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exam by code {ExamCode}", maDeThi);
                return BaseResponseDto<DethiDto>.FailureResult("An error occurred");
            }
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetActiveExamsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await _dethiRepository.GetActiveExamsAsync(cancellationToken);
                var result = _mapper.Map<List<DethiDto>>(exams);
                return BaseResponseDto<List<DethiDto>>.SuccessResult(result, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active exams");
                return BaseResponseDto<List<DethiDto>>.FailureResult("An error occurred", new List<string> { ex.Message });
            }
        }

        public async Task<BaseResponseDto<List<DethiDto>>> GetAllExamsAsync(string? trangThai = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await _dethiRepository.GetAllExamsAsync(trangThai, cancellationToken);
                var dtos = exams.Select(e => _mapper.Map<DethiDto>(e)).ToList();
                return new BaseResponseDto<List<DethiDto>> { Success = true, Data = dtos };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto<List<DethiDto>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<DethiDto>> UpdateExamAsync(int examId, UpdateDethiDto updateDto, int nguoiCapNhat, CancellationToken cancellationToken = default)
        {
            try
            {
                KyThi? kyThi = null;
                if (updateDto.KyThiId.HasValue)
                {
                    kyThi = await _context.Set<KyThi>()
                        .Include(k => k.KhoaPhong)
                        .FirstOrDefaultAsync(k => k.Id == updateDto.KyThiId.Value, cancellationToken);
                    if (kyThi != null)
                    {
                        updateDto.TenDeThi = kyThi.TenKyThi;
                        updateDto.ThoiGianBatDau = kyThi.ThoiGianBatDau;
                        updateDto.TrangThai = kyThi.TrangThai;

                        if (kyThi.ThoiGianBatDau.HasValue && kyThi.ThoiGianKetThuc.HasValue)
                        {
                            var diff = kyThi.ThoiGianKetThuc.Value - kyThi.ThoiGianBatDau.Value;
                            var duration = (int)diff.TotalMinutes;
                            if (duration > 0)
                            {
                                updateDto.ThoiGianLamBai = duration;
                            }
                        }
                    }
                }

                var permissionValidation = await _validationService.ValidateExamPermissionAsync(examId, nguoiCapNhat, "UPDATE", cancellationToken);
                if (!permissionValidation.IsValid)
                    return BaseResponseDto<DethiDto>.FailureResult("Access denied", permissionValidation.Errors);

                using var transaction = await _dethiRepository.BeginTransactionAsync();

                try
                {
                    var existingExam = await _dethiRepository.GetByIdAsync(examId, cancellationToken);
                    if (existingExam == null)
                        return BaseResponseDto<DethiDto>.FailureResult("Exam not found");

                    existingExam.MaDeThi = updateDto.MaDeThi;
                    existingExam.TenDeThi = updateDto.TenDeThi;
                    existingExam.ThoiGianLamBai = updateDto.ThoiGianLamBai ?? existingExam.ThoiGianLamBai ?? 60;
                    existingExam.ThoiGianBatDau = updateDto.ThoiGianBatDau;
                    existingExam.TrangThai = updateDto.TrangThai;
                    existingExam.KyThiId = updateDto.KyThiId;
                    if (kyThi != null)
                    {
                        existingExam.KhoaPhong = kyThi.KhoaPhong?.TenKhoa;
                    }
                    existingExam.NguoiCapNhat = nguoiCapNhat;
                    existingExam.NgayCapNhat = DateTime.UtcNow;

                    if (updateDto.DanhSachIdCauHoi.Any())
                    {
                        if (kyThi != null && kyThi.TongSoCauHoi.HasValue && updateDto.DanhSachIdCauHoi.Count != kyThi.TongSoCauHoi.Value)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            var errorMsg = updateDto.DanhSachIdCauHoi.Count < kyThi.TongSoCauHoi.Value
                                ? $"Số lượng câu hỏi chưa đủ, còn thiếu {kyThi.TongSoCauHoi.Value - updateDto.DanhSachIdCauHoi.Count} câu hỏi"
                                : $"Số lượng câu hỏi vượt quá yêu cầu, thừa {updateDto.DanhSachIdCauHoi.Count - kyThi.TongSoCauHoi.Value} câu hỏi";
                            return BaseResponseDto<DethiDto>.FailureResult(errorMsg);
                        }

                        await _dethiRepository.UpdateExamQuestionsAsync(examId, updateDto.DanhSachIdCauHoi);
                        existingExam.TongDiem = updateDto.DanhSachIdCauHoi.Count;
                    }

                    await _dethiRepository.UpdateAsync(existingExam);
                    await transaction.CommitAsync(cancellationToken);

                    var result = _mapper.Map<DethiDto>(existingExam);
                    return BaseResponseDto<DethiDto>.SuccessResult(result, "Cập nhật thành công");
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
                return BaseResponseDto<DethiDto>.FailureResult("Có lỗi xảy ra");
            }
        }

        public async Task<BaseResponseDto<DethiDto>> UpdateExamStatusAsync(int examId, string trangThai, int nguoiCapNhat, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(trangThai) || !System.Text.RegularExpressions.Regex.IsMatch(trangThai, "^(Draft|Active|Inactive|Archived)$"))
                    return BaseResponseDto<DethiDto>.FailureResult("Invalid status value");

                var exam = await _dethiRepository.GetByIdAsync(examId, cancellationToken);
                if (exam == null)
                    return BaseResponseDto<DethiDto>.FailureResult("Exam not found");

                exam.TrangThai = trangThai;
                exam.NguoiCapNhat = nguoiCapNhat;
                exam.NgayCapNhat = DateTime.UtcNow;

                await _dethiRepository.UpdateAsync(exam);

                var result = _mapper.Map<DethiDto>(exam);
                return BaseResponseDto<DethiDto>.SuccessResult(result, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam status {ExamId}", examId);
                return BaseResponseDto<DethiDto>.FailureResult("Có lỗi xảy ra");
            }
        }

        public async Task<BaseResponseDto> DeleteExamAsync(int examId, int nguoiXoa, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _dethiRepository.DeleteAsync(examId);
                if (!deleted)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy đề thi" };
                return new BaseResponseDto { Success = true, Message = "Đã xóa đề thi" };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeactivateExamAsync(int examId, int nguoiCapNhat, CancellationToken cancellationToken = default)
        {
            try
            {
                var exam = await _dethiRepository.GetByIdAsync(examId, cancellationToken);
                if (exam == null)
                    return BaseResponseDto.FailureResult("Exam not found");

                exam.TrangThai = "Inactive";
                exam.NguoiCapNhat = nguoiCapNhat;
                exam.NgayCapNhat = DateTime.UtcNow;
                await _dethiRepository.UpdateAsync(exam);
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

        private static string CalculateExamChecksum(CreateDethiDto createDto)
        {
            var data = $"{createDto.MaDeThi}|{createDto.TenDeThi}|{createDto.ThoiGianLamBai ?? 60}|{createDto.KhoaPhong}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}
