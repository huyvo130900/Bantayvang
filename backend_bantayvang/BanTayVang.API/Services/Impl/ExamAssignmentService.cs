using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class ExamAssignmentService : Services.Interfaces.IExamAssignmentService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamAssignmentService> _logger;

        public ExamAssignmentService(BanTayVangDbContext context, ILogger<ExamAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<List<ExamAssignmentDto>>> GetAssignmentsByExamAsync(int examId)
        {
            try
            {
                var assignments = await _context.ExamAssignments
                    .Where(a => a.ExamId == examId && a.IsActive)
                    .Include(a => a.Exam)
                    .Include(a => a.User)
                    .Select(a => new ExamAssignmentDto
                    {
                        Id = a.Id,
                        ExamId = a.ExamId,
                        MaDeThi = a.Exam!.MaDeThi,
                        TenDeThi = a.Exam.TenDeThi,
                        UserId = a.UserId,
                        Username = a.User!.TenDangNhap,
                        FullName = a.User.HoTen,
                        AssignedAt = a.AssignedAt,
                        CustomStartTime = a.CustomStartTime,
                        ExtraMinutes = a.ExtraMinutes,
                        IsActive = a.IsActive,
                        Note = a.Note,
                        TrangThai = "Pending"
                    })
                    .ToListAsync();

                return new BaseResponseDto<List<ExamAssignmentDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = assignments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments");
                return new BaseResponseDto<List<ExamAssignmentDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamAssignmentDto>()
                };
            }
        }

        /// <summary>
        /// Lấy danh sách đề thi của học sinh, kèm trạng thái và kết quả bài thi
        /// </summary>
        public async Task<BaseResponseDto<List<ExamAssignmentDto>>> GetAssignmentsByUserAsync(int userId)
        {
            try
            {
                var assignments = await _context.ExamAssignments
                    .Where(a => a.UserId == userId && a.IsActive)
                    .Include(a => a.Exam)
                    .Include(a => a.User)
                    .ToListAsync();

                var result = new List<ExamAssignmentDto>();

                foreach (var a in assignments)
                {
                    // Tìm bài thi đã làm (nếu có)
                    var baithi = await _context.Baithis
                        .Where(b => b.IdTaiKhoan == userId && b.IdDeThi == a.ExamId)
                        .OrderByDescending(b => b.ThoiGianBatDau)
                        .FirstOrDefaultAsync();

                    var trangThai = "Pending";
                    int? baithiId = null;
                    double? diemSo = null;
                    double? tongDiem = null;
                    int? soCauDung = null;
                    int? tongSoCau = null;
                    DateTime? ngayHoanThanh = null;
                    bool? datYeuCau = null;
                    string? thoiGianLamBai = null;
                    string? thoiGianKetThuc = null;

                    if (baithi != null)
                    {
                        baithiId = baithi.Id;
                        trangThai = baithi.TrangThai ?? "InProgress";
                        diemSo = baithi.TongDiem;
                        soCauDung = baithi.SoCauDung;
                        tongSoCau = baithi.TongSoCau;
                        ngayHoanThanh = baithi.ThoiGianNop;
                        thoiGianLamBai = baithi.ThoiGianBatDau?.ToString("yyyy-MM-ddTHH:mm:ss");
                        thoiGianKetThuc = baithi.ThoiGianNop?.ToString("yyyy-MM-ddTHH:mm:ss");

                        // Lấy tổng điểm của đề thi
                        tongDiem = a.Exam?.TongDiem;

                        // Đạt nếu >= 50% tổng điểm
                        if (baithi.TongDiem.HasValue && a.Exam?.TongDiem > 0)
                        {
                            datYeuCau = baithi.TongDiem >= (a.Exam.TongDiem * 0.5);
                        }

                        // Redact scores if not published
                        var congBo = baithi.CongBoRieng || (a.Exam?.CongBoKetQua ?? false);
                        if (!congBo)
                        {
                            diemSo = null;
                            soCauDung = null;
                            datYeuCau = null;
                        }
                    }

                    result.Add(new ExamAssignmentDto
                    {
                        Id = a.Id,
                        ExamId = a.ExamId,
                        MaDeThi = a.Exam?.MaDeThi,
                        TenDeThi = a.Exam?.TenDeThi,
                        UserId = a.UserId,
                        Username = a.User?.TenDangNhap,
                        FullName = a.User?.HoTen,
                        AssignedAt = a.AssignedAt,
                        CustomStartTime = a.CustomStartTime,
                        ExtraMinutes = a.ExtraMinutes,
                        IsActive = a.IsActive,
                        Note = a.Note,
                        TrangThai = trangThai,
                        BaithiId = baithiId,
                        DiemSo = diemSo,
                        TongDiem = tongDiem,
                        SoCauDung = soCauDung,
                        TongSoCau = tongSoCau,
                        NgayHoanThanh = ngayHoanThanh,
                        DatYeuCau = datYeuCau,
                        ThoiGianBatDau = thoiGianLamBai,
                        ThoiGianKetThuc = thoiGianKetThuc,
                        ThoiGianLamBai = a.Exam?.ThoiGianLamBai
                    });
                }

                return new BaseResponseDto<List<ExamAssignmentDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user assignments");
                return new BaseResponseDto<List<ExamAssignmentDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamAssignmentDto>()
                };
            }
        }

        public async Task<BaseResponseDto<int>> AssignUsersToExamAsync(CreateExamAssignmentDto dto)
        {
            try
            {
                var exam = await _context.Dethis.FindAsync(dto.ExamId);
                if (exam == null)
                    return new BaseResponseDto<int> { Success = false, Message = "Không tìm thấy đề thi" };

                int count = 0;
                foreach (var userId in dto.UserIds)
                {
                    var exists = await _context.ExamAssignments
                        .AnyAsync(a => a.ExamId == dto.ExamId && a.UserId == userId);
                    if (exists) continue;

                    var assignment = new ExamAssignment
                    {
                        ExamId = dto.ExamId,
                        UserId = userId,
                        AssignedAt = DateTime.UtcNow,
                        CustomStartTime = dto.CustomStartTime,
                        IsActive = true,
                        Note = dto.Note
                    };
                    _context.ExamAssignments.Add(assignment);
                    count++;
                }
                await _context.SaveChangesAsync();
                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = $"Đã phân công {count} thí sinh",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning users");
                return new BaseResponseDto<int> { Success = false, Message = ex.Message, Data = 0 };
            }
        }

        public async Task<BaseResponseDto> RemoveAssignmentAsync(int assignmentId)
        {
            try
            {
                var assignment = await _context.ExamAssignments.FindAsync(assignmentId);
                if (assignment == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy phân công" };
                assignment.IsActive = false;
                await _context.SaveChangesAsync();
                return new BaseResponseDto { Success = true, Message = "Đã hủy phân công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<bool>> IsUserAssignedAsync(int examId, int userId)
        {
            try
            {
                var assigned = await _context.ExamAssignments
                    .AnyAsync(a => a.ExamId == examId && a.UserId == userId && a.IsActive);
                return new BaseResponseDto<bool> { Success = true, Message = "Thành công", Data = assigned };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking assignment");
                return new BaseResponseDto<bool> { Success = false, Message = ex.Message, Data = false };
            }
        }

        public async Task<BaseResponseDto> ExtendExamTimeAsync(ExtendExamTimeDto dto)
        {
            try
            {
                var baithi = await _context.Baithis.FindAsync(dto.BaiThiId);
                if (baithi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };
                if (baithi.TrangThai != "InProgress")
                    return new BaseResponseDto { Success = false, Message = "Chỉ gia hạn bài thi đang làm" };

                var assignment = await _context.ExamAssignments
                    .FirstOrDefaultAsync(a => a.ExamId == baithi.IdDeThi && a.UserId == baithi.IdTaiKhoan);

                if (assignment == null)
                {
                    assignment = new ExamAssignment
                    {
                        ExamId = baithi.IdDeThi ?? 0,
                        UserId = baithi.IdTaiKhoan ?? 0,
                        AssignedAt = DateTime.UtcNow,
                        ExtraMinutes = dto.AdditionalMinutes,
                        IsActive = true,
                        Note = $"Gia hạn: {dto.Reason}"
                    };
                    _context.ExamAssignments.Add(assignment);
                }
                else
                {
                    assignment.ExtraMinutes = (assignment.ExtraMinutes ?? 0) + dto.AdditionalMinutes;
                    assignment.Note = $"{assignment.Note}; Gia hạn thêm {dto.AdditionalMinutes}p: {dto.Reason}";
                }
                await _context.SaveChangesAsync();
                return new BaseResponseDto { Success = true, Message = $"Đã gia hạn thêm {dto.AdditionalMinutes} phút" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending exam time");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }
    }
}
