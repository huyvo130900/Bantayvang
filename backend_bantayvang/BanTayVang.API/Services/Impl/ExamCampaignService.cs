// ============================================================
// FILE: Services/Impl/ExamCampaignService.cs
// FIX: Trạng thái ExamCampaign và CaThi tự động theo thời gian
//
// LỖI: Status lưu tĩnh trong DB ("DangChuanBi", "ChuaBatDau")
//   → Dù đã đến giờ, trạng thái không thay đổi → UI hiện "Chưa bắt đầu"
//   → FIX: Tính trạng thái động trong MapToDto, không cần cron job
// ============================================================

using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.DTOs.ExamCampaign;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>Hàng trung gian dùng chung cho 2 nhánh (theo khoa / theo danh sách chỉ định) của GetEligibilityAsync.</summary>
    internal record EligibleUserRow(int Id, string? FullName, string? EmployeeCode, string? Department);

    public class ExamCampaignService : Services.Interfaces.IExamCampaignService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamCampaignService> _logger;
        private readonly IExamAssignmentService _examAssignmentService;

        public ExamCampaignService(BanTayVangDbContext context, ILogger<ExamCampaignService> logger, IExamAssignmentService examAssignmentService)
        {
            _context = context;
            _logger = logger;
            _examAssignmentService = examAssignmentService;
        }

        public async Task<BaseResponseDto<List<ExamCampaignDto>>> GetAllAsync(string? status = null, int? currentUserId = null)
        {
            try
            {
                var query = _context.Set<ExamCampaign>()
                    .Include(k => k.ExamCampaignDepartments).ThenInclude(kd => kd.Department)
                    .AsQueryable();

                if (currentUserId.HasValue)
                {
                    var currentUser = await _context.Users.FindAsync(currentUserId.Value);
                    // BUG FIX: this endpoint is now reachable by any authenticated role (previously
                    // ManagementOnly blocked students entirely - see controller comment). Only
                    // RoleId==3 (Student) was scoped to their own department; RoleId==6
                    // (ThiSinhNgoai, external candidates - also has a real Department string set at
                    // registration approval time) fell through with no filter at all, which would
                    // have shown every department's campaigns the moment students could reach this
                    // endpoint. Apply the same department scoping to both roles.
                    //
                    // A campaign with NO row in ExamCampaignDepartments means "not restricted to any
                    // specific department" (same as the old DepartmentId == null convention) and is
                    // visible to everyone, matching prior behavior.
                    if (currentUser != null && (currentUser.RoleId == 3 || currentUser.RoleId == 6))
                    {
                        // BUG FIX: a campaign in "AssignedList" mode almost always has an EMPTY
                        // ExamCampaignDepartments (nothing to fill it with - eligibility comes from
                        // ExamAssignment instead), so the "empty = visible to everyone" department
                        // fallback above was matching it too - every student saw it in their list
                        // and only got rejected at the very last step (start-exam), instead of the
                        // list correctly reflecting who was actually invited.
                        var assignedCampaignIds = await _context.ExamAssignments
                            .Where(a => a.IsActive && a.UserId == currentUserId.Value && a.Exam!.ExamCampaignId != null)
                            .Select(a => a.Exam!.ExamCampaignId!.Value)
                            .Distinct()
                            .ToListAsync();

                        if (!string.IsNullOrEmpty(currentUser.Department))
                        {
                            var userKhoa = await _context.Departments
                                .FirstOrDefaultAsync(kp => kp.DepartmentName == currentUser.Department);
                            if (userKhoa != null)
                            {
                                query = query.Where(k => k.AccessMode == "AssignedList"
                                    ? assignedCampaignIds.Contains(k.Id)
                                    : !k.ExamCampaignDepartments.Any() || k.ExamCampaignDepartments.Any(kd => kd.DepartmentId == userKhoa.Id));
                            }
                            else
                            {
                                query = query.Where(k => k.AccessMode == "AssignedList"
                                    ? assignedCampaignIds.Contains(k.Id)
                                    : !k.ExamCampaignDepartments.Any() || k.OrganizedBy == currentUser.Department);
                            }
                        }
                        else
                        {
                            query = query.Where(k => k.AccessMode == "AssignedList"
                                ? assignedCampaignIds.Contains(k.Id)
                                : !k.ExamCampaignDepartments.Any());
                        }
                    }
                    else if (currentUser != null && currentUser.RoleId == 5)
                    {
                        query = query.Where(k => currentUser.DeptManagerDeptId != null && k.ExamCampaignDepartments.Any(kd => kd.DepartmentId == currentUser.DeptManagerDeptId));
                    }
                }

                var examCampaigns = await query.OrderByDescending(k => k.CreatedAt).ToListAsync();
                
                var examPapers = await _context.ExamPapers.Where(d => d.ExamCampaignId != null).ToListAsync();
                var examPapersGrouped = examPapers.GroupBy(d => d.ExamCampaignId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = examCampaigns.Select(k => {
                    var dto = MapToDto(k);
                    if (examPapersGrouped.TryGetValue(k.Id, out var linkedExamPapers))
                    {
                        dto.ExamPaperCodes = linkedExamPapers.Select(d => d.ExamPaperCode ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                        dto.TotalExamPapers = linkedExamPapers.Count;
                        dto.ExamPaperCode = dto.ExamPaperCodes.FirstOrDefault();
                    }
                    return dto;
                }).ToList();

                // Filter theo trạng thái TÍNH TOÁN (không phải DB)
                if (!string.IsNullOrEmpty(status))
                    result = result.Where(k => k.Status == status).ToList();

                return new BaseResponseDto<List<ExamCampaignDto>> { Success = true, Message = "Thành công", Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ky thi list");
                return new BaseResponseDto<List<ExamCampaignDto>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamCampaignDto>> GetByIdAsync(int id)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>()
                    .Include(k => k.ExamCampaignDepartments).ThenInclude(kd => kd.Department)
                    .FirstOrDefaultAsync(k => k.Id == id);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var examPapers = await _context.ExamPapers
                    .Where(d => d.ExamCampaignId == id)
                    .ToListAsync();

                var dto = MapToDto(examCampaign);
                dto.ExamPaperCodes = examPapers.Select(d => d.ExamPaperCode ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                dto.TotalExamPapers = examPapers.Count;
                dto.ExamPaperCode = dto.ExamPaperCodes.FirstOrDefault();

                return new BaseResponseDto<ExamCampaignDto> { Success = true, Message = "Thành công", Data = dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ky thi");
                return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamCampaignDto>> CreateAsync(CreateExamCampaignDto dto, int createdBy)
        {
            try
            {
                if (dto.StartTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.EndTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var startLocal = dto.StartTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.StartTime.Value.ToLocalTime() 
                    : dto.StartTime.Value;

                var endLocal = dto.EndTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.EndTime.Value.ToLocalTime() 
                    : dto.EndTime.Value;

                // BUG FIX: the "is this time in the past" check below used to compare startLocal/endLocal
                // (derived via .ToLocalTime(), which depends on the DEPLOYMENT SERVER's OS timezone setting)
                // against `now` (a hardcoded fake-VN UtcNow+7h value). Whenever the server's OS timezone is
                // NOT set to Vietnam (e.g. the common default of UTC in containers/cloud VMs), .ToLocalTime()
                // is a no-op and startLocal/endLocal stay TRUE UTC, so comparing them to fake-VN `now` was off
                // by ~7 hours and could wrongly reject valid future start/end times as "in the past". Compare
                // the true-UTC instants directly instead - this is correct regardless of server OS timezone.
                var nowUtc = DateTime.UtcNow;
                var startUtc = dto.StartTime.Value.Kind == DateTimeKind.Utc
                    ? dto.StartTime.Value
                    : DateTime.SpecifyKind(dto.StartTime.Value, DateTimeKind.Utc);
                var endUtc = dto.EndTime.Value.Kind == DateTimeKind.Utc
                    ? dto.EndTime.Value
                    : DateTime.SpecifyKind(dto.EndTime.Value, DateTimeKind.Utc);

                if (startUtc < nowUtc.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endUtc < nowUtc.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                if (await _context.Set<ExamCampaign>().AnyAsync(k => k.CampaignCode == dto.CampaignCode))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Mã kỳ thi đã tồn tại" };

                var departmentIds = (dto.DepartmentIds ?? new List<int>()).Distinct().ToList();
                var departments = departmentIds.Count > 0
                    ? await _context.Set<Department>().Where(k => departmentIds.Contains(k.Id)).ToListAsync()
                    : new List<Department>();
                string? donViToChuc = BuildOrganizedBy(departments);

                var examCampaign = new ExamCampaign
                {
                    CampaignCode = dto.CampaignCode,
                    CampaignName = dto.CampaignName,
                    Description = dto.Description,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    OrganizedBy = donViToChuc,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = "DangChuanBi",
                    AccessMode = dto.AccessMode == "AssignedList" ? "AssignedList" : "Department",
                    IsPracticeMode = dto.IsPracticeMode,
                    MinPassQuestions = dto.MinPassQuestions,
                    TotalQuestions = dto.TotalQuestions,
                    DurationMinutes = dto.DurationMinutes
                };

                foreach (var khoa in departments)
                {
                    examCampaign.ExamCampaignDepartments.Add(new ExamCampaignDepartment { Department = khoa });
                }

                _context.Set<ExamCampaign>().Add(examCampaign);
                await _context.SaveChangesAsync();

                return new BaseResponseDto<ExamCampaignDto> { Success = true, Message = "Tạo kỳ thi thành công", Data = MapToDto(examCampaign) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ky thi");
                return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamCampaignDto>> UpdateAsync(int id, UpdateExamCampaignDto dto)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>()
                    .Include(k => k.ExamCampaignDepartments)
                    .FirstOrDefaultAsync(k => k.Id == id);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (dto.StartTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.EndTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var startLocal = dto.StartTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.StartTime.Value.ToLocalTime() 
                    : dto.StartTime.Value;

                var endLocal = dto.EndTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.EndTime.Value.ToLocalTime() 
                    : dto.EndTime.Value;

                // BUG FIX: see identical fix + rationale in CreateAsync above - startLocal/endLocal
                // depend on the deployment server's OS timezone via .ToLocalTime(), so comparing them
                // against the fake-VN `now` for a past/future check is unreliable. Use true UTC instead.
                var nowUtc = DateTime.UtcNow;
                var startUtc = dto.StartTime.Value.Kind == DateTimeKind.Utc
                    ? dto.StartTime.Value
                    : DateTime.SpecifyKind(dto.StartTime.Value, DateTimeKind.Utc);
                var endUtc = dto.EndTime.Value.Kind == DateTimeKind.Utc
                    ? dto.EndTime.Value
                    : DateTime.SpecifyKind(dto.EndTime.Value, DateTimeKind.Utc);

                var dbStartLocal = examCampaign.StartTime.HasValue 
                    ? (examCampaign.StartTime.Value.Kind == DateTimeKind.Utc 
                        ? examCampaign.StartTime.Value.ToLocalTime() 
                        : (examCampaign.StartTime.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(examCampaign.StartTime.Value, DateTimeKind.Utc).ToLocalTime() 
                            : examCampaign.StartTime.Value))
                    : (DateTime?)null;

                var dbEndLocal = examCampaign.EndTime.HasValue 
                    ? (examCampaign.EndTime.Value.Kind == DateTimeKind.Utc 
                        ? examCampaign.EndTime.Value.ToLocalTime() 
                        : (examCampaign.EndTime.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(examCampaign.EndTime.Value, DateTimeKind.Utc).ToLocalTime() 
                            : examCampaign.EndTime.Value))
                    : (DateTime?)null;

                // Check if date has actually changed with a minute-precision threshold (0.1 minutes)
                bool startChanged = dbStartLocal == null || Math.Abs((startLocal - dbStartLocal.Value).TotalMinutes) > 0.1;
                bool endChanged = dbEndLocal == null || Math.Abs((endLocal - dbEndLocal.Value).TotalMinutes) > 0.1;

                if (startChanged && startUtc < nowUtc.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endChanged && endUtc < nowUtc.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                var departmentIds = (dto.DepartmentIds ?? new List<int>()).Distinct().ToList();
                var departments = departmentIds.Count > 0
                    ? await _context.Set<Department>().Where(k => departmentIds.Contains(k.Id)).ToListAsync()
                    : new List<Department>();
                string? donViToChuc = BuildOrganizedBy(departments);

                examCampaign.CampaignName = dto.CampaignName;
                examCampaign.Description = dto.Description;
                examCampaign.StartTime = dto.StartTime;
                examCampaign.EndTime = dto.EndTime;
                examCampaign.OrganizedBy = donViToChuc;
                examCampaign.Status = dto.Status;
                examCampaign.AccessMode = dto.AccessMode == "AssignedList" ? "AssignedList" : "Department";
                examCampaign.IsPracticeMode = dto.IsPracticeMode;
                examCampaign.UpdatedAt = DateTime.UtcNow.AddHours(7);
                examCampaign.MinPassQuestions = dto.MinPassQuestions;
                examCampaign.TotalQuestions = dto.TotalQuestions;
                examCampaign.DurationMinutes = dto.DurationMinutes;

                // Sync related exams (ExamPaper) properties if time/name has changed
                var relatedExams = await _context.Set<ExamPaper>()
                    .Where(d => d.ExamCampaignId == id)
                    .ToListAsync();

                foreach (var exam in relatedExams)
                {
                    exam.StartTime = dto.StartTime;
                    if (dto.DurationMinutes.HasValue && dto.DurationMinutes.Value > 0)
                    {
                        exam.DurationMinutes = dto.DurationMinutes.Value;
                    }
                    
                    if (!string.IsNullOrEmpty(exam.ExamPaperName) && exam.ExamPaperName.Contains(" - "))
                    {
                        var parts = exam.ExamPaperName.Split(new[] { " - " }, StringSplitOptions.None);
                        var suffix = parts.Last();
                        exam.ExamPaperName = $"{dto.CampaignName} - {suffix}";
                    }
                    else
                    {
                        exam.ExamPaperName = dto.CampaignName;
                    }

                    exam.Department = donViToChuc;
                }

                // Replace the department set wholesale - simpler and safer than diffing, and this
                // is an admin/dept-manager config action, not a hot path.
                foreach (var oldLink in examCampaign.ExamCampaignDepartments.ToList())
                {
                    examCampaign.ExamCampaignDepartments.Remove(oldLink);
                    _context.Set<ExamCampaignDepartment>().Remove(oldLink);
                }
                foreach (var khoa in departments)
                {
                    examCampaign.ExamCampaignDepartments.Add(new ExamCampaignDepartment { Department = khoa });
                }

                await _context.SaveChangesAsync();

                return new BaseResponseDto<ExamCampaignDto> { Success = true, Message = "Cập nhật thành công", Data = MapToDto(examCampaign) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ky thi");
                return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> UpdateStatusAsync(int id, string status)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FindAsync(id);
                if (examCampaign == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                examCampaign.Status = status;
                // BUG FIX (mục 45): dùng đúng quy ước giờ VN giả (AddHours(7)) như 2 chỗ set
                // UpdatedAt/CreatedAt khác trong cùng file, tránh lệch hiển thị 7 tiếng so với
                // các hành động Create/Update khác trên cùng bản ghi.
                examCampaign.UpdatedAt = DateTime.UtcNow.AddHours(7);
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = $"Đã chuyển trạng thái sang: {status}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Báo cáo "ai đủ điều kiện thi / ai đã thi" cho 1 kỳ thi, kèm kết quả thi mới nhất (nếu
        /// có) của từng người trong ExamSubmissions. Roster phụ thuộc AccessMode:
        /// - "Department": roster theo (các) khoa gán cho kỳ thi (ExamCampaignDepartments); không
        ///   gán khoa nào (danh sách rỗng) giữ hành vi cũ "Tất cả khoa".
        /// - "AssignedList": roster là đúng những người có ExamAssignment active với 1 trong các
        ///   đề thi của kỳ thi này - đây chính là nhóm ValidateStartExamAsync sẽ cho phép thi.
        /// </summary>
        public async Task<BaseResponseDto<List<ExamCampaignEligibilityDto>>> GetEligibilityAsync(int examCampaignId)
        {
            try
            {
                var campaign = await _context.Set<ExamCampaign>()
                    .Include(k => k.ExamCampaignDepartments).ThenInclude(kd => kd.Department)
                    .FirstOrDefaultAsync(k => k.Id == examCampaignId);
                if (campaign == null)
                    return new BaseResponseDto<List<ExamCampaignEligibilityDto>> { Success = false, Message = "Không tìm thấy kỳ thi" };

                List<EligibleUserRow> eligibleUsers;
                if (campaign.AccessMode == "AssignedList")
                {
                    var examPaperIds = await _context.ExamPapers
                        .Where(p => p.ExamCampaignId == examCampaignId)
                        .Select(p => p.Id)
                        .ToListAsync();
                    var assignedUserIds = await _context.ExamAssignments
                        .Where(a => a.IsActive && examPaperIds.Contains(a.ExamId))
                        .Select(a => a.UserId)
                        .Distinct()
                        .ToListAsync();
                    eligibleUsers = await _context.Users
                        .Where(u => assignedUserIds.Contains(u.Id))
                        .Select(u => new EligibleUserRow(u.Id, u.FullName, u.EmployeeCode, u.Department))
                        .ToListAsync();
                }
                else
                {
                    var deptNames = campaign.ExamCampaignDepartments.Select(kd => kd.Department!.DepartmentName).ToList();

                    var usersQuery = _context.Users.Where(u => u.RoleId == 3 || u.RoleId == 6);
                    if (deptNames.Count > 0)
                        usersQuery = usersQuery.Where(u => u.Department != null && deptNames.Contains(u.Department));

                    eligibleUsers = await usersQuery
                        .Select(u => new EligibleUserRow(u.Id, u.FullName, u.EmployeeCode, u.Department))
                        .ToListAsync();
                }

                var submissions = await _context.ExamSubmissions
                    .Where(s => s.ExamCampaignId == examCampaignId && s.UserId != null)
                    .OrderByDescending(s => s.SubmitTime)
                    .ToListAsync();
                // Mỗi người có thể thi lại nhiều lần - chỉ lấy lần nộp gần nhất cho báo cáo này.
                var latestByUser = submissions
                    .GroupBy(s => s.UserId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var result = eligibleUsers.Select(u =>
                {
                    latestByUser.TryGetValue(u.Id, out var latest);
                    return new ExamCampaignEligibilityDto
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        EmployeeCode = u.EmployeeCode,
                        Department = u.Department,
                        HasSubmitted = latest != null,
                        SubmissionStatus = latest?.Status,
                        TotalScore = latest?.TotalScore,
                        SubmitTime = latest?.SubmitTime
                    };
                })
                .OrderBy(d => d.Department).ThenBy(d => d.FullName)
                .ToList();

                return new BaseResponseDto<List<ExamCampaignEligibilityDto>> { Success = true, Message = "Thành công", Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting eligibility for ExamCampaign {ExamCampaignId}", examCampaignId);
                return new BaseResponseDto<List<ExamCampaignEligibilityDto>> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Đọc file Excel/CSV chứa danh sách mã nhân viên/tên đăng nhập và gán quyền thi cho đúng
        /// những người đó vào 1 kỳ thi ở chế độ "Chỉ định danh sách" (AccessMode = AssignedList).
        /// Chỉ TÌM tài khoản đã tồn tại (không tạo mới) rồi gán ExamAssignment cho từng đề thi của
        /// kỳ thi - đây chính là điều kiện ValidateStartExamAsync kiểm tra khi thí sinh bắt đầu thi.
        /// </summary>
        public async Task<BaseResponseDto<AssignFromExcelResultDto>> AssignFromExcelAsync(int examCampaignId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = "File không hợp lệ hoặc rỗng." };

            try
            {
                var campaign = await _context.Set<ExamCampaign>().FindAsync(examCampaignId);
                if (campaign == null)
                    return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (campaign.AccessMode != "AssignedList")
                    return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = "Kỳ thi phải ở chế độ \"Danh sách chỉ định\" mới có thể úp danh sách." };

                var examPaperIds = await _context.ExamPapers
                    .Where(p => p.ExamCampaignId == examCampaignId)
                    .Select(p => p.Id)
                    .ToListAsync();
                if (examPaperIds.Count == 0)
                    return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = "Kỳ thi chưa có đề thi nào. Vui lòng sinh đề thi trước khi úp danh sách." };

                var codes = ReadCodesFromFile(file);
                if (codes.Count == 0)
                    return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = "Không đọc được mã nhân viên/tài khoản nào từ file." };

                var matchedUsers = await _context.Users
                    .Where(u => codes.Contains(u.EmployeeCode!) || codes.Contains(u.Username!))
                    .Select(u => new { u.Id, u.EmployeeCode, u.Username })
                    .ToListAsync();

                var matchedCodes = new HashSet<string>(
                    matchedUsers.SelectMany(u => new[] { u.EmployeeCode, u.Username })
                        .Where(c => !string.IsNullOrEmpty(c))!
                        .Select(c => c!),
                    StringComparer.OrdinalIgnoreCase);
                var notFoundCodes = codes.Where(c => !matchedCodes.Contains(c)).ToList();
                var matchedUserIds = matchedUsers.Select(u => u.Id).Distinct().ToList();

                foreach (var examPaperId in examPaperIds)
                {
                    await _examAssignmentService.AssignUsersToExamAsync(new CreateExamAssignmentDto
                    {
                        ExamId = examPaperId,
                        UserIds = matchedUserIds,
                        Note = "Úp từ danh sách Excel"
                    });
                }

                return new BaseResponseDto<AssignFromExcelResultDto>
                {
                    Success = true,
                    Message = $"Đã gán quyền thi cho {matchedUserIds.Count}/{codes.Count} người trong danh sách.",
                    Data = new AssignFromExcelResultDto
                    {
                        TotalRows = codes.Count,
                        MatchedUserCount = matchedUserIds.Count,
                        NotFoundCodes = notFoundCodes
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning users from Excel for ExamCampaign {ExamCampaignId}", examCampaignId);
                return new BaseResponseDto<AssignFromExcelResultDto> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Đọc cột đầu tiên chứa mã nhân viên/tên đăng nhập từ file .csv hoặc .xlsx. Chấp nhận cả
        /// file có dòng tiêu đề (tự nhận diện "mã nhân viên"/"tài khoản"/"username") lẫn file chỉ
        /// có 1 cột danh sách thuần không tiêu đề - đúng dạng người dùng thường copy/paste ra.
        /// </summary>
        private static List<string> ReadCodesFromFile(IFormFile file)
        {
            var codes = new List<string>();
            using var stream = file.OpenReadStream();

            bool LooksLikeHeader(string text)
            {
                var t = text.Trim().ToLowerInvariant();
                return t.Contains("mã nhân viên") || t.Contains("ma nhan vien") || t.Contains("tài khoản") ||
                       t.Contains("tai khoan") || t.Contains("username") || t.Contains("employee");
            }

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(stream);
                string? line;
                bool first = true;
                while ((line = reader.ReadLine()) != null)
                {
                    var cell = line.Split(',')[0].Trim().Trim('"');
                    if (first)
                    {
                        first = false;
                        if (LooksLikeHeader(cell)) continue;
                    }
                    if (!string.IsNullOrWhiteSpace(cell)) codes.Add(cell);
                }
            }
            else
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int row = 1; row <= lastRow; row++)
                {
                    var cell = ws.Cell(row, 1).GetString().Trim();
                    if (row == 1 && LooksLikeHeader(cell)) continue;
                    if (!string.IsNullOrWhiteSpace(cell)) codes.Add(cell);
                }
            }

            return codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<BaseResponseDto> DeleteAsync(int id)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FirstOrDefaultAsync(k => k.Id == id);
                if (examCampaign == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                var computedStatus = ComputeExamCampaignStatus(examCampaign);
                if (computedStatus == "DangDienRa")
                    return new BaseResponseDto { Success = false, Message = "Không thể xóa kỳ thi đang diễn ra" };

                _context.Set<ExamCampaign>().Remove(examCampaign);
                await _context.SaveChangesAsync();
                return new BaseResponseDto { Success = true, Message = "Xóa kỳ thi thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ky thi");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }



        // ============================================================
        // FIX: Tính trạng thái ĐỘNG từ thời gian thực
        // Không phụ thuộc vào giá trị lưu trong DB
        // ============================================================

        /// <summary>
        /// Tính trạng thái ExamCampaign theo giờ hiện tại
        /// </summary>
        private static string ComputeExamCampaignStatus(ExamCampaign k)
        {
            var nowUtc = DateTime.UtcNow;

            // Kỳ thi luyện tập tồn tại vĩnh viễn - không tự chuyển DaKetThuc theo EndTime như kỳ
            // thi thật. Vẫn tôn trọng TamDung/DaKetThuc nếu admin CHỦ ĐỘNG đặt (vd để tạm khóa
            // hoặc dừng hẳn/xóa kỳ thi luyện tập), chỉ bỏ qua phần tự động tính theo EndTime.
            if (k.IsPracticeMode)
                return (k.Status == "TamDung" || k.Status == "DaKetThuc") ? k.Status : "DangDienRa";

            // Nếu trạng thái trong DB là DaKetThuc hoặc TamDung -> tôn trọng tuyệt đối
            if (k.Status == "DaKetThuc" || k.Status == "TamDung")
                return k.Status;

            var startUtc = k.StartTime.HasValue
                ? (k.StartTime.Value.Kind == DateTimeKind.Utc 
                    ? k.StartTime.Value 
                    : DateTime.SpecifyKind(k.StartTime.Value, DateTimeKind.Utc))
                : (DateTime?)null;

            var endUtc = k.EndTime.HasValue 
                ? (k.EndTime.Value.Kind == DateTimeKind.Utc 
                    ? k.EndTime.Value 
                    : DateTime.SpecifyKind(k.EndTime.Value, DateTimeKind.Utc))
                : (DateTime?)null;

            // Nếu thời gian kết thúc đã qua -> DaKetThuc
            if (endUtc.HasValue && nowUtc > endUtc.Value)
                return "DaKetThuc";

            // Nếu trạng thái trong DB được set là DangDienRa -> tôn trọng
            if (k.Status == "DangDienRa")
                return "DangDienRa";

            // Tính toán động theo thời gian
            if (startUtc.HasValue && nowUtc >= startUtc.Value)
                return "DangDienRa";
            if (startUtc.HasValue && nowUtc < startUtc.Value)
                return "DangChuanBi";

            return k.Status ?? "DangChuanBi";
        }

        /// <summary>
        /// Tên hiển thị "Đơn vị tổ chức": tên khoa duy nhất nếu 1 khoa, liệt kê nếu vài khoa,
        /// hoặc rút gọn "X và N khoa khác" nếu quá nhiều - tránh chuỗi quá dài trên UI.
        /// </summary>
        private static string? BuildOrganizedBy(List<Department> departments)
        {
            if (departments.Count == 0) return null;
            if (departments.Count <= 3) return string.Join(", ", departments.Select(d => d.DepartmentName));
            return $"{departments[0].DepartmentName} và {departments.Count - 1} khoa khác";
        }

        private static ExamCampaignDto MapToDto(ExamCampaign k)
        {
            var departments = k.ExamCampaignDepartments.Select(kd => kd.Department).Where(d => d != null).Select(d => d!).ToList();
            return new ExamCampaignDto
            {
                Id = k.Id,
                CampaignCode = k.CampaignCode,
                CampaignName = k.CampaignName,
                Description = k.Description,
                DepartmentIds = departments.Select(d => d.Id).ToList(),
                DepartmentNames = departments.Select(d => d.DepartmentName).ToList(),
                AccessMode = k.AccessMode,
                IsPracticeMode = k.IsPracticeMode,
                Status = ComputeExamCampaignStatus(k), // FIX: tính động
                StartTime = k.StartTime.HasValue ? DateTime.SpecifyKind(k.StartTime.Value, DateTimeKind.Utc) : null,
                EndTime = k.EndTime.HasValue ? DateTime.SpecifyKind(k.EndTime.Value, DateTimeKind.Utc) : null,
                OrganizedBy = k.OrganizedBy,
                CreatedAt = k.CreatedAt,
                MinPassQuestions = k.MinPassQuestions,
                TotalQuestions = k.TotalQuestions,
                DurationMinutes = k.DurationMinutes
            };
        }

        // ============================================================
        // MULTIPLE EXAMS CHECK & GENERATION
        // ============================================================

        /// <summary>
        /// Danh sách khoa dùng để lọc ngân hàng câu hỏi khi sinh đề, hoặc null nếu không giới hạn
        /// khoa. Ưu tiên <see cref="ExamGenerationConfigDto.DepartmentNames"/> (hỗ trợ 1-n khoa,
        /// đúng với việc kỳ thi giờ có thể gán nhiều khoa); nếu trống mới rơi về
        /// <see cref="ExamGenerationConfigDto.Department"/> (chuỗi đơn, dùng cho luồng Quản lý
        /// khoa - luôn đúng 1 khoa của chính họ).
        /// </summary>
        private static List<string>? ResolveDeptFilter(ExamGenerationConfigDto config)
        {
            var names = (config.DepartmentNames ?? new List<string>())
                .Where(d => !string.IsNullOrWhiteSpace(d) && d.Trim() != "Tất cả các khoa" && d.Trim() != "Tất cả khoa phòng")
                .Distinct()
                .ToList();
            if (names.Count > 0) return names;

            var single = config.Department;
            if (!string.IsNullOrEmpty(single) && single.Trim() != "Tất cả các khoa" && single.Trim() != "Tất cả khoa phòng")
                return new List<string> { single };

            return null;
        }

        public async Task<BaseResponseDto<ExamCheckResultDto>> CheckExamsAvailabilityAsync(int examCampaignId, ExamGenerationConfigDto config)
        {
            try
            {
                var result = new ExamCheckResultDto { CanGenerate = true };

                var targetDeptNames = ResolveDeptFilter(config);

                var query = _context.Questions
                    .Include(c => c.QuestionCategory)
                    .Include(q => q.QuestionOptions)
                    .Where(q => q.IsDeleted != true);

                if (targetDeptNames != null)
                {
                    query = query.Where(q => q.Department != null && targetDeptNames.Contains(q.Department));
                }

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                // Group and count questions
                // Hỗ trợ tất cả dạng tên loại câu hỏi Tự luận được lưu trong DB
                bool IsEssay(Question q) => EssayQuestionHelper.IsEssay(q); // KHONG hardcode QuestionCategoryId == 3 nua - ID danh muc co the khac nhau giua cac moi truong/seed data
                bool IsMC(Question q) => !IsEssay(q);

                string NormalizeDifficulty(string? difficulty)
                {
                    if (string.IsNullOrEmpty(difficulty)) return "Dễ";
                    var d = difficulty.ToLower().Trim();
                    // Hỗ trợ giá trị số lưu trong DB: "1"=Dễ, "2"=Trung bình, "3"=Khó
                    if (d == "3" || d.Contains("khó") || d.Contains("hard")) return "Khó";
                    if (d == "2" || d.Contains("trung bình") || d.Contains("medium") || d.Contains("vừa")) return "Trung bình";
                    return "Dễ";
                }

                var bankEasyEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Dễ");
                var bankMediumEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Trung bình");
                var bankHardEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Khó");

                var bankEasyMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Dễ");
                var bankMediumMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Trung bình");
                var bankHardMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Khó");

                var bankEasy = bankEasyEssay + bankEasyMC;
                var bankMedium = bankMediumEssay + bankMediumMC;
                var bankHard = bankHardEssay + bankHardMC;

                var bankMC = bankEasyMC + bankMediumMC + bankHardMC;
                var bankEssay = bankEasyEssay + bankMediumEssay + bankHardEssay;

                // BUG FIX: each exam paper needs its OWN distinct set of questions - previously
                // this only checked the bank had enough questions for ONE exam's worth, then
                // GenerateExamsForCampaignAsync independently re-shuffled and re-drew from the SAME
                // full pool for every exam in the batch, so "Đề 1"/"Đề 2"/... could end up with
                // heavily overlapping (or, if the bank was exactly sized for one exam, IDENTICAL)
                // question sets - defeating the whole point of giving different candidates
                // different papers. Multiply every requirement by NumberOfExams so we only report
                // "can generate" when the bank truly has enough DISTINCT questions for all of them.
                var numExams = Math.Max(1, config.NumberOfExams);
                var reqEasy = config.EasyQuestions * numExams;
                var reqMedium = config.MediumQuestions * numExams;
                var reqHard = config.HardQuestions * numExams;
                var reqMC = config.MultipleChoiceQuestions * numExams;
                var reqEssay = config.EssayQuestions * numExams;

                if (bankEasy < reqEasy)
                {
                    result.Warnings.Add($"Thiếu {reqEasy - bankEasy} câu hỏi Dễ để {numExams} đề không trùng nhau (Cần: {reqEasy}, Ngân hàng có: {bankEasy})");
                    result.CanGenerate = false;
                }
                if (bankMedium < reqMedium)
                {
                    result.Warnings.Add($"Thiếu {reqMedium - bankMedium} câu hỏi Trung bình để {numExams} đề không trùng nhau (Cần: {reqMedium}, Ngân hàng có: {bankMedium})");
                    result.CanGenerate = false;
                }
                if (bankHard < reqHard)
                {
                    result.Warnings.Add($"Thiếu {reqHard - bankHard} câu hỏi Khó để {numExams} đề không trùng nhau (Cần: {reqHard}, Ngân hàng có: {bankHard})");
                    result.CanGenerate = false;
                }
                if (bankMC < reqMC)
                {
                    result.Warnings.Add($"Thiếu {reqMC - bankMC} câu hỏi Trắc nghiệm để {numExams} đề không trùng nhau (Cần: {reqMC}, Ngân hàng có: {bankMC})");
                    result.CanGenerate = false;
                }
                if (bankEssay < reqEssay)
                {
                    result.Warnings.Add($"Thiếu {reqEssay - bankEssay} câu hỏi Tự luận để {numExams} đề không trùng nhau (Cần: {reqEssay}, Ngân hàng có: {bankEssay})");
                    result.CanGenerate = false;
                }

                // If global targets are satisfied, verify cell distribution (each cell's per-exam
                // count times the number of exams must still fit the bank for that cell).
                if (result.CanGenerate)
                {
                    bool canDistribute = false;
                    for (int ee = 0; ee <= config.EssayQuestions; ee++)
                    {
                        for (int me = 0; me <= config.EssayQuestions - ee; me++)
                        {
                            int he = config.EssayQuestions - ee - me;
                            int em = config.EasyQuestions - ee;
                            int mm = config.MediumQuestions - me;
                            int hm = config.HardQuestions - he;

                            if (em >= 0 && mm >= 0 && hm >= 0 && (em + mm + hm == config.MultipleChoiceQuestions))
                            {
                                if (ee * numExams <= bankEasyEssay &&
                                    me * numExams <= bankMediumEssay &&
                                    he * numExams <= bankHardEssay &&
                                    em * numExams <= bankEasyMC &&
                                    mm * numExams <= bankMediumMC &&
                                    hm * numExams <= bankHardMC)
                                {
                                    canDistribute = true;
                                    break;
                                }
                            }
                        }
                        if (canDistribute) break;
                    }

                    if (!canDistribute)
                    {
                        result.Warnings.Add($"Không thể phân bổ câu hỏi vừa khớp giữa các thể loại và độ khó từ ngân hàng câu hỏi để tạo {numExams} đề không trùng nhau.");
                        result.CanGenerate = false;
                    }
                }

                return new BaseResponseDto<ExamCheckResultDto> { Success = true, Message = "Kiểm tra thành công", Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking exam generation availability");
                return new BaseResponseDto<ExamCheckResultDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> GenerateExamsForCampaignAsync(int examCampaignId, ExamGenerationConfigDto config, int createdBy)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FindAsync(examCampaignId);
                if (examCampaign == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                // BUG FIX: the "no duplicate questions across papers" guarantee above only holds
                // WITHIN a single call to this method (it shuffles/slices one snapshot of the
                // question bank across all N papers being created together). Nothing stopped this
                // endpoint from being called again for a campaign that already has exam papers -
                // whether from an admin re-running it to add more papers, a double-click, or a slow-
                // network retry. A second call re-queries the FULL bank with no knowledge of which
                // questions the first call already used, so the new batch of papers can duplicate
                // (or fully repeat) questions from the existing batch - exactly the outcome this
                // whole mechanism exists to prevent. Require existing papers to be removed first.
                var alreadyHasExamPapers = await _context.ExamPapers.AnyAsync(d => d.ExamCampaignId == examCampaignId);
                if (alreadyHasExamPapers)
                {
                    return new BaseResponseDto { Success = false, Message = "Kỳ thi này đã có đề thi được sinh trước đó. Vui lòng xóa các đề thi hiện có trước khi sinh đề mới, để tránh trùng câu hỏi giữa các đề." };
                }

                if (examCampaign.TotalQuestions.HasValue && config.TotalQuestions != examCampaign.TotalQuestions.Value)
                    return new BaseResponseDto { Success = false, Message = $"Số lượng câu hỏi của đề thi ({config.TotalQuestions}) không khớp với tổng số câu hỏi đã thiết lập cho kỳ thi ({examCampaign.TotalQuestions.Value})" };

                var targetDeptNames = ResolveDeptFilter(config);

                // 1. Run check (reuses the same config, so it sees the exact same dept filter)
                var checkRes = await CheckExamsAvailabilityAsync(examCampaignId, config);
                if (!checkRes.Success || checkRes.Data == null || !checkRes.Data.CanGenerate)
                {
                    var errors = checkRes.Data?.Warnings ?? new List<string> { "Không đủ câu hỏi trong ngân hàng" };
                    return new BaseResponseDto { Success = false, Message = "Kiểm tra ngân hàng câu hỏi không đạt yêu cầu", Errors = errors };
                }

                // 2. Resolve cells
                int targetEE = 0, targetME = 0, targetHE = 0;
                int targetEM = 0, targetMM = 0, targetHM = 0;
                bool solved = false;

                for (int ee = 0; ee <= config.EssayQuestions; ee++)
                {
                    for (int me = 0; me <= config.EssayQuestions - ee; me++)
                    {
                        int he = config.EssayQuestions - ee - me;
                        int em = config.EasyQuestions - ee;
                        int mm = config.MediumQuestions - me;
                        int hm = config.HardQuestions - he;

                        if (em >= 0 && mm >= 0 && hm >= 0 && (em + mm + hm == config.MultipleChoiceQuestions))
                        {
                            targetEE = ee; targetME = me; targetHE = he;
                            targetEM = em; targetMM = mm; targetHM = hm;
                            solved = true;
                            break;
                        }
                    }
                    if (solved) break;
                }

                if (!solved)
                    return new BaseResponseDto { Success = false, Message = "Lỗi thuật toán phân bổ câu hỏi" };

                // 3. Fetch questions matching criteria
                var query = _context.Questions
                    .Include(c => c.QuestionCategory)
                    .Include(q => q.QuestionOptions)
                    .Where(q => q.IsDeleted != true);

                if (targetDeptNames != null)
                    query = query.Where(q => q.Department != null && targetDeptNames.Contains(q.Department));

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                // Hỗ trợ tất cả dạng tên loại câu hỏi Tự luận được lưu trong DB
                bool IsEssay(Question q) => EssayQuestionHelper.IsEssay(q); // KHONG hardcode QuestionCategoryId == 3 nua - ID danh muc co the khac nhau giua cac moi truong/seed data
                bool IsMC(Question q) => !IsEssay(q);

                string NormalizeDifficulty(string? difficulty)
                {
                    if (string.IsNullOrEmpty(difficulty)) return "Dễ";
                    var d = difficulty.ToLower().Trim();
                    // Hỗ trợ giá trị số lưu trong DB: "1"=Dễ, "2"=Trung bình, "3"=Khó
                    if (d == "3" || d.Contains("khó") || d.Contains("hard")) return "Khó";
                    if (d == "2" || d.Contains("trung bình") || d.Contains("medium") || d.Contains("vừa")) return "Trung bình";
                    return "Dễ";
                }

                var easyEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Dễ").OrderBy(_ => Guid.NewGuid()).ToList();
                var medEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Trung bình").OrderBy(_ => Guid.NewGuid()).ToList();
                var hardEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.Difficulty) == "Khó").OrderBy(_ => Guid.NewGuid()).ToList();

                var easyMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Dễ").OrderBy(_ => Guid.NewGuid()).ToList();
                var medMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Trung bình").OrderBy(_ => Guid.NewGuid()).ToList();
                var hardMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.Difficulty) == "Khó").OrderBy(_ => Guid.NewGuid()).ToList();

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create n exams
                    for (int i = 0; i < config.NumberOfExams; i++)
                    {
                        var examPaperCode = $"{examCampaign.CampaignName.Replace(" ", "_")}_DE_{i + 1}_{Guid.NewGuid().ToString("N")[..6]}".ToUpper();
                        var examPaperName = $"{examCampaign.CampaignName} - Đề {i + 1}";

                        // Determine duration from ky thi or default to 60
                        int duration = 60;
                        if (examCampaign.DurationMinutes.HasValue && examCampaign.DurationMinutes.Value > 0)
                        {
                            duration = examCampaign.DurationMinutes.Value;
                        }

                        var examPaper = new ExamPaper
                        {
                            ExamPaperCode = examPaperCode,
                            ExamPaperName = examPaperName,
                            StartTime = examCampaign.StartTime,
                            DurationMinutes = duration,
                            Status = "Active",
                            Department = targetDeptNames != null ? string.Join(", ", targetDeptNames) : null,
                            CreatedAt = DateTime.UtcNow.AddHours(7),
                            CreatedBy = createdBy,
                            ExamCampaignId = examCampaign.Id,
                            LinkTruyCap = $"/exam/{examPaperCode}",
                            TotalScore = config.TotalQuestions,
                            IsResultPublished = false
                        };

                        _context.ExamPapers.Add(examPaper);
                        await _context.SaveChangesAsync();

                        // BUG FIX: this used to re-shuffle and re-Take from the SAME full pool on
                        // every iteration, so exam papers could share most or all of their
                        // questions (identical if the bank was exactly sized for one exam). The
                        // pools were already randomly shuffled once above, so give each exam its
                        // own consecutive slice (Skip(i*count)) instead - every exam gets a
                        // disjoint set of questions, guaranteed by CheckExamsAvailabilityAsync
                        // having already confirmed the bank holds enough for NumberOfExams papers.
                        var examQuestions = new List<Question>();
                        examQuestions.AddRange(easyEssayPool.Skip(i * targetEE).Take(targetEE));
                        examQuestions.AddRange(medEssayPool.Skip(i * targetME).Take(targetME));
                        examQuestions.AddRange(hardEssayPool.Skip(i * targetHE).Take(targetHE));

                        examQuestions.AddRange(easyMCPool.Skip(i * targetEM).Take(targetEM));
                        examQuestions.AddRange(medMCPool.Skip(i * targetMM).Take(targetMM));
                        examQuestions.AddRange(hardMCPool.Skip(i * targetHM).Take(targetHM));

                        // Add to ExamPaperQuestion
                        foreach (var q in examQuestions)
                        {
                            var dc = new ExamPaperQuestion
                            {
                                ExamPaperId = examPaper.Id,
                                QuestionId = q.Id
                            };
                            _context.ExamPaperQuestions.Add(dc);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully generated {Count} randomized exams for ExamCampaign {ExamCampaignId}", config.NumberOfExams, examCampaignId);
                    return new BaseResponseDto { Success = true, Message = $"Đã tạo thành công {config.NumberOfExams} đề thi ngẫu nhiên cho kỳ thi." };
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error generating exams for ExamCampaign {ExamCampaignId}", examCampaignId);
                    return new BaseResponseDto { Success = false, Message = "Lỗi cơ sở dữ liệu khi tạo bộ đề thi." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exams for ExamCampaign {ExamCampaignId}", examCampaignId);
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }
    }
}
