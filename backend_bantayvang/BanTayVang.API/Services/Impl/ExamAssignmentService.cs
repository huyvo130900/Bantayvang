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
                    .IgnoreQueryFilters()
                    .Where(a => a.ExamId == examId && a.IsActive)
                    .Include(a => a.Exam)
                    .Include(a => a.User)
                    .Select(a => new ExamAssignmentDto
                    {
                        Id = a.Id,
                        ExamId = a.ExamId,
                        ExamPaperCode = a.Exam!.ExamPaperCode,
                        ExamPaperName = a.Exam.ExamPaperName,
                        UserId = a.UserId,
                        Username = a.User!.Username,
                        FullName = a.User.FullName,
                        AssignedAt = a.AssignedAt,
                        CustomStartTime = a.CustomStartTime,
                        ExtraMinutes = a.ExtraMinutes,
                        IsActive = a.IsActive,
                        Note = a.Note,
                        Status = "Pending"
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
                    .IgnoreQueryFilters()
                    .Where(a => a.UserId == userId && a.IsActive)
                    .Include(a => a.Exam)
                    .Include(a => a.User)
                    .ToListAsync();

                var result = new List<ExamAssignmentDto>();

                foreach (var a in assignments)
                {
                    // Tìm bài thi đã làm (nếu có)
                    var examSubmission = await _context.ExamSubmissions
                        .Where(b => b.UserId == userId && b.ExamPaperId == a.ExamId)
                        .OrderByDescending(b => b.StartTime)
                        .FirstOrDefaultAsync();

                    var status = "Pending";
                    int? baithiId = null;
                    double? diemSo = null;
                    double? totalScore = null;
                    int? correctAnswers = null;
                    int? tongSoCau = null;
                    DateTime? ngayHoanThanh = null;
                    bool? datYeuCau = null;
                    string? durationMinutes = null;
                    string? thoiGianKetThuc = null;

                    if (examSubmission != null)
                    {
                        baithiId = examSubmission.Id;
                        status = examSubmission.Status ?? "InProgress";
                        diemSo = examSubmission.TotalScore;
                        correctAnswers = examSubmission.CorrectAnswers;
                        tongSoCau = examSubmission.TotalQuestions;
                        ngayHoanThanh = examSubmission.SubmitTime;
                        durationMinutes = examSubmission.StartTime?.ToString("yyyy-MM-ddTHH:mm:ss");
                        thoiGianKetThuc = examSubmission.SubmitTime?.ToString("yyyy-MM-ddTHH:mm:ss");

                        // Lấy tổng điểm của đề thi
                        totalScore = a.Exam?.TotalScore;

                        // Đạt nếu >= 50% tổng điểm
                        if (examSubmission.TotalScore.HasValue && a.Exam?.TotalScore > 0)
                        {
                            datYeuCau = examSubmission.TotalScore >= (a.Exam.TotalScore * 0.5);
                        }

                        // Redact scores if not published
                        var congBo = examSubmission.CongBoRieng || (a.Exam?.IsResultPublished ?? false);
                        if (!congBo)
                        {
                            diemSo = null;
                            correctAnswers = null;
                            datYeuCau = null;
                        }
                    }

                    result.Add(new ExamAssignmentDto
                    {
                        Id = a.Id,
                        ExamId = a.ExamId,
                        ExamPaperCode = a.Exam?.ExamPaperCode,
                        ExamPaperName = a.Exam?.ExamPaperName,
                        UserId = a.UserId,
                        Username = a.User?.Username,
                        FullName = a.User?.FullName,
                        AssignedAt = a.AssignedAt,
                        CustomStartTime = a.CustomStartTime,
                        ExtraMinutes = a.ExtraMinutes,
                        IsActive = a.IsActive,
                        Note = a.Note,
                        Status = status,
                        BaithiId = baithiId,
                        DiemSo = diemSo,
                        TotalScore = totalScore,
                        CorrectAnswers = correctAnswers,
                        TotalQuestions = tongSoCau,
                        NgayHoanThanh = ngayHoanThanh,
                        DatYeuCau = datYeuCau,
                        StartTime = durationMinutes,
                        EndTime = thoiGianKetThuc,
                        DurationMinutes = a.Exam?.DurationMinutes
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
                var exam = await _context.ExamPapers.FindAsync(dto.ExamId);
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
                var examSubmission = await _context.ExamSubmissions.FindAsync(dto.BaiThiId);
                if (examSubmission == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };
                if (examSubmission.Status != "InProgress")
                    return new BaseResponseDto { Success = false, Message = "Chỉ gia hạn bài thi đang làm" };

                var assignment = await _context.ExamAssignments
                    .FirstOrDefaultAsync(a => a.ExamId == examSubmission.ExamPaperId && a.UserId == examSubmission.UserId);

                if (assignment == null)
                {
                    assignment = new ExamAssignment
                    {
                        ExamId = examSubmission.ExamPaperId ?? 0,
                        UserId = examSubmission.UserId ?? 0,
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
