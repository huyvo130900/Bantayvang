using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Helpers;
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
                    int? examSubmissionId = null;
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
                        examSubmissionId = examSubmission.Id;
                        status = examSubmission.Status ?? "InProgress";
                        diemSo = examSubmission.TotalScore;
                        correctAnswers = examSubmission.CorrectAnswers;
                        tongSoCau = examSubmission.TotalQuestions;
                        ngayHoanThanh = examSubmission.SubmitTime;
                        durationMinutes = examSubmission.StartTime?.ToString("yyyy-MM-ddTHH:mm:ss");
                        thoiGianKetThuc = examSubmission.SubmitTime?.ToString("yyyy-MM-ddTHH:mm:ss");

                        // Lấy tổng điểm của đề thi
                        totalScore = a.Exam?.TotalScore;

                        // BUG FIX: this compared examSubmission.TotalScore (always normalized to a
                        // 0-10 scale by ExamSubmissionService.CalculateTotalScore) against half of
                        // ExamPaper.TotalScore, which UpdateExamAsync/CreateExamAsync set to the RAW
                        // QUESTION COUNT (10, 20, 50...), not a 0-10 value - comparing a normalized
                        // score against a raw-count-derived threshold. It only ever produced a
                        // sensible result by coincidence for a paper with exactly 10 questions; for
                        // any other count it silently made passing impossible (>10 questions) or
                        // trivially easy (<10 questions). Use the same CorrectAnswers/MinPassQuestions
                        // comparison already established as the canonical pass rule elsewhere
                        // (ExamService.GetMyResultsAsync).
                        // BUG FIX: precedence used to be ExamPaper-first / ExamCampaign-second (and
                        // left datYeuCau null/undetermined when nothing was configured), the reverse
                        // of the canonical rule used everywhere else (GradingService.cs x3,
                        // ExamService.GetMyResultsAsync). Now uses the same shared PassRuleHelper
                        // instead of a locally copy-pasted version, to avoid the two silently
                        // drifting the next time this rule needs to change.
                        int? campaignMinPassQuestions = null;
                        if (examSubmission.ExamCampaignId.HasValue)
                        {
                            campaignMinPassQuestions = await _context.Set<ExamCampaign>()
                                .Where(c => c.Id == examSubmission.ExamCampaignId.Value)
                                .Select(c => c.MinPassQuestions)
                                .FirstOrDefaultAsync();
                        }
                        datYeuCau = PassRuleHelper.ComputePass(examSubmission.CorrectAnswers, campaignMinPassQuestions, a.Exam?.MinPassQuestions);

                        // Redact scores if not published
                        var isPublished = examSubmission.IsIndividualResultPublished || (a.Exam?.IsResultPublished ?? false);
                        if (!isPublished)
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
                        ExamSubmissionId = examSubmissionId,
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
                    // BUG FIX: this used to check AnyAsync(...) with no IsActive filter, so a user
                    // who was ever assigned then removed (RemoveAssignmentAsync only soft-deletes via
                    // IsActive=false, never deletes the row) could NEVER be re-assigned again - this
                    // loop would find their old inactive row, silently `continue`, and report success
                    // with a lower count while doing nothing for that user. Now reactivates the
                    // existing row instead of treating "a row exists" as "already assigned".
                    var existing = await _context.ExamAssignments
                        .FirstOrDefaultAsync(a => a.ExamId == dto.ExamId && a.UserId == userId);
                    if (existing != null)
                    {
                        if (existing.IsActive) continue;

                        existing.IsActive = true;
                        existing.AssignedAt = DateTime.UtcNow;
                        existing.CustomStartTime = dto.CustomStartTime;
                        existing.Note = dto.Note;
                        count++;
                        continue;
                    }

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
                var examSubmission = await _context.ExamSubmissions.FindAsync(dto.ExamSubmissionId);
                if (examSubmission == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };
                if (examSubmission.Status != "InProgress")
                    return new BaseResponseDto { Success = false, Message = "Chỉ gia hạn bài thi đang làm" };

                var assignmentId = await _context.ExamAssignments
                    .Where(a => a.ExamId == examSubmission.ExamPaperId && a.UserId == examSubmission.UserId)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefaultAsync();

                // BUG FIX: this used to load the ExamAssignment entity, read-modify-write
                // ExtraMinutes/Note in memory, then SaveChangesAsync - a classic lost-update.
                // Two supervisors (or one double-clicking) extending the same student's time
                // concurrently would both read the same starting ExtraMinutes, and the second
                // SaveChangesAsync would silently overwrite the first extension instead of
                // stacking. ExecuteUpdateAsync's SetProperty expressions are translated to a
                // single atomic "SET X = X + @p" UPDATE, so concurrent extensions always stack.
                if (assignmentId.HasValue)
                {
                    await _context.ExamAssignments
                        .Where(a => a.Id == assignmentId.Value)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(a => a.ExtraMinutes, a => (a.ExtraMinutes ?? 0) + dto.AdditionalMinutes)
                            .SetProperty(a => a.Note, a => (a.Note ?? "") + $"; Gia hạn thêm {dto.AdditionalMinutes}p: {dto.Reason}"));
                }
                else
                {
                    var assignment = new ExamAssignment
                    {
                        ExamId = examSubmission.ExamPaperId ?? 0,
                        UserId = examSubmission.UserId ?? 0,
                        AssignedAt = DateTime.UtcNow,
                        ExtraMinutes = dto.AdditionalMinutes,
                        IsActive = true,
                        Note = $"Gia hạn: {dto.Reason}"
                    };
                    _context.ExamAssignments.Add(assignment);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Lost the race to create the first assignment row for this pair (the
                        // UX_ExamAssignments_User_Exam unique index rejected it) - someone else's
                        // concurrent extend-time just created it, so fold this grant into theirs.
                        _context.Entry(assignment).State = EntityState.Detached;
                        await _context.ExamAssignments
                            .Where(a => a.ExamId == examSubmission.ExamPaperId && a.UserId == examSubmission.UserId)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(a => a.ExtraMinutes, a => (a.ExtraMinutes ?? 0) + dto.AdditionalMinutes)
                                .SetProperty(a => a.Note, a => (a.Note ?? "") + $"; Gia hạn thêm {dto.AdditionalMinutes}p: {dto.Reason}"));
                    }
                }
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
