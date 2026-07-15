// ============================================================
// FILE: Services/Impl/ExamCampaignService.cs
// FIX: Trạng thái ExamCampaign và CaThi tự động theo thời gian
//
// LỖI: Status lưu tĩnh trong DB ("DangChuanBi", "ChuaBatDau")
//   → Dù đã đến giờ, trạng thái không thay đổi → UI hiện "Chưa bắt đầu"
//   → FIX: Tính trạng thái động trong MapToDto, không cần cron job
// ============================================================

using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.ExamCampaign;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class ExamCampaignService : Services.Interfaces.IExamCampaignService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<ExamCampaignService> _logger;

        public ExamCampaignService(BanTayVangDbContext context, ILogger<ExamCampaignService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<List<ExamCampaignDto>>> GetAllAsync(string? status = null, int? currentUserId = null)
        {
            try
            {
                var query = _context.Set<ExamCampaign>()
                    .Include(k => k.Department)
                    .AsQueryable();

                if (currentUserId.HasValue)
                {
                    var currentUser = await _context.Users.FindAsync(currentUserId.Value);
                    if (currentUser != null && currentUser.RoleId == 3 && !string.IsNullOrEmpty(currentUser.Department))
                    {
                        var userKhoa = await _context.Departments
                            .FirstOrDefaultAsync(kp => kp.DepartmentName == currentUser.Department);
                        if (userKhoa != null)
                        {
                            query = query.Where(k => k.DepartmentId == userKhoa.Id || k.DepartmentId == null);
                        }
                        else
                        {
                            query = query.Where(k => k.OrganizedBy == currentUser.Department || (k.Department != null && k.Department.DepartmentName == currentUser.Department) || k.DepartmentId == null);
                        }
                    }
                    else if (currentUser != null && currentUser.RoleId == 5)
                    {
                        query = query.Where(k => k.DepartmentId == currentUser.DeptManagerDeptId);
                    }
                }

                var examCampaigns = await query.OrderByDescending(k => k.CreatedAt).ToListAsync();
                
                var examPapers = await _context.ExamPapers.Where(d => d.KyThiId != null).ToListAsync();
                var dethisGrouped = examPapers.GroupBy(d => d.KyThiId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = examCampaigns.Select(k => {
                    var dto = MapToDto(k);
                    if (dethisGrouped.TryGetValue(k.Id, out var linkedDethis))
                    {
                        dto.DanhSachMaDeThi = linkedDethis.Select(d => d.ExamPaperCode ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                        dto.SoLuongDeThi = linkedDethis.Count;
                        dto.ExamPaperCode = dto.DanhSachMaDeThi.FirstOrDefault();
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
                    .Include(k => k.Department)
                    .FirstOrDefaultAsync(k => k.Id == id);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var examPapers = await _context.ExamPapers
                    .Where(d => d.KyThiId == id)
                    .ToListAsync();

                var dto = MapToDto(examCampaign);
                dto.DanhSachMaDeThi = examPapers.Select(d => d.ExamPaperCode ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                dto.SoLuongDeThi = examPapers.Count;
                dto.ExamPaperCode = dto.DanhSachMaDeThi.FirstOrDefault();

                return new BaseResponseDto<ExamCampaignDto> { Success = true, Message = "Thành công", Data = dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ky thi");
                return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamCampaignDto>> CreateAsync(CreateKyThiDto dto, int createdBy)
        {
            try
            {
                if (dto.StartTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.EndTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var now = DateTime.Now;
                var startLocal = dto.StartTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.StartTime.Value.ToLocalTime() 
                    : dto.StartTime.Value;

                var endLocal = dto.EndTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.EndTime.Value.ToLocalTime() 
                    : dto.EndTime.Value;

                if (startLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                if (await _context.Set<ExamCampaign>().AnyAsync(k => k.CampaignCode == dto.CampaignCode))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Mã kỳ thi đã tồn tại" };

                string? donViToChuc = null;
                if (dto.DepartmentId.HasValue)
                {
                    var khoa = await _context.Set<Department>().FindAsync(dto.DepartmentId.Value);
                    if (khoa != null)
                    {
                        donViToChuc = khoa.DepartmentName;
                    }
                }

                var examCampaign = new ExamCampaign
                {
                    CampaignCode = dto.CampaignCode,
                    CampaignName = dto.CampaignName,
                    Description = dto.Description,
                    DepartmentId = dto.DepartmentId,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    OrganizedBy = donViToChuc,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    Status = "DangChuanBi", 
                    MinPassQuestions = dto.MinPassQuestions,
                    TotalQuestions = dto.TotalQuestions,
                    DurationMinutes = dto.DurationMinutes
                };

                _context.Set<ExamCampaign>().Add(examCampaign);
                await _context.SaveChangesAsync();

                if (examCampaign.DepartmentId.HasValue)
                {
                    await _context.Entry(examCampaign).Reference(k => k.Department).LoadAsync();
                }

                return new BaseResponseDto<ExamCampaignDto> { Success = true, Message = "Tạo kỳ thi thành công", Data = MapToDto(examCampaign) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ky thi");
                return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<ExamCampaignDto>> UpdateAsync(int id, UpdateKyThiDto dto)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FindAsync(id);
                if (examCampaign == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (dto.StartTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.EndTime == null)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var now = DateTime.Now;
                var startLocal = dto.StartTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.StartTime.Value.ToLocalTime() 
                    : dto.StartTime.Value;

                var endLocal = dto.EndTime.Value.Kind == DateTimeKind.Utc 
                    ? dto.EndTime.Value.ToLocalTime() 
                    : dto.EndTime.Value;

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

                if (startChanged && startLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endChanged && endLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<ExamCampaignDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                string? donViToChuc = null;
                if (dto.DepartmentId.HasValue)
                {
                    var khoa = await _context.Set<Department>().FindAsync(dto.DepartmentId.Value);
                    if (khoa != null)
                    {
                        donViToChuc = khoa.DepartmentName;
                    }
                }

                examCampaign.CampaignName = dto.CampaignName;
                examCampaign.Description = dto.Description;
                examCampaign.DepartmentId = dto.DepartmentId;
                examCampaign.StartTime = dto.StartTime;
                examCampaign.EndTime = dto.EndTime;
                examCampaign.OrganizedBy = donViToChuc;
                examCampaign.Status = dto.Status;
                examCampaign.UpdatedAt = DateTime.Now;
                examCampaign.MinPassQuestions = dto.MinPassQuestions;
                examCampaign.TotalQuestions = dto.TotalQuestions;
                examCampaign.DurationMinutes = dto.DurationMinutes;

                // Sync related exams (ExamPaper) properties if time/name has changed
                var relatedExams = await _context.Set<ExamPaper>()
                    .Where(d => d.KyThiId == id)
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

                await _context.SaveChangesAsync();

                if (examCampaign.DepartmentId.HasValue)
                {
                    await _context.Entry(examCampaign).Reference(k => k.Department).LoadAsync();
                }

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
                examCampaign.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = $"Đã chuyển trạng thái sang: {status}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeleteAsync(int id)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FirstOrDefaultAsync(k => k.Id == id);
                if (examCampaign == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                var computedStatus = ComputeKyThiStatus(examCampaign);
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
        private static string ComputeKyThiStatus(ExamCampaign k)
        {
            var nowUtc = DateTime.UtcNow;
            
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

        private static ExamCampaignDto MapToDto(ExamCampaign k)
        {
            return new ExamCampaignDto
            {
                Id = k.Id,
                CampaignCode = k.CampaignCode,
                CampaignName = k.CampaignName,
                Description = k.Description,
                DepartmentId = k.DepartmentId,
                DepartmentName = k.Department?.DepartmentName,
                Status = ComputeKyThiStatus(k), // FIX: tính động
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

        public async Task<BaseResponseDto<ExamCheckResultDto>> CheckExamsAvailabilityAsync(int kyThiId, ExamGenerationConfigDto config)
        {
            try
            {
                var result = new ExamCheckResultDto { CanGenerate = true };

                string? targetKhoaPhong = config.Department;
                if (!string.IsNullOrEmpty(targetKhoaPhong) && 
                    (targetKhoaPhong.Trim() == "Tất cả các khoa" || targetKhoaPhong.Trim() == "Tất cả khoa phòng"))
                {
                    targetKhoaPhong = null;
                }

                var query = _context.Questions
                    .Include(c => c.IdLoaiCauHoiNavigation)
                    .Where(q => q.DaXoa != true);

                if (!string.IsNullOrEmpty(targetKhoaPhong))
                {
                    query = query.Where(q => q.Department == targetKhoaPhong);
                }

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                // Group and count questions
                bool IsEssay(Question q) => q.QuestionCategoryId == 3 || (q.IdLoaiCauHoiNavigation != null && q.IdLoaiCauHoiNavigation.CategoryName != null && q.IdLoaiCauHoiNavigation.CategoryName.ToLower().Contains("tự luận"));
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

                // Target requirements
                var reqEasy = config.SoCauEasy * config.SoLuongDe;
                var reqMedium = config.SoCauMedium * config.SoLuongDe;
                var reqHard = config.SoCauHard * config.SoLuongDe;
                var reqMC = config.SoCauMC * config.SoLuongDe;
                var reqEssay = config.SoCauEssay * config.SoLuongDe;

                if (bankEasy < reqEasy)
                {
                    result.Warnings.Add($"Thiếu {reqEasy - bankEasy} câu hỏi Dễ (Yêu cầu: {reqEasy}, Ngân hàng có: {bankEasy})");
                    result.CanGenerate = false;
                }
                if (bankMedium < reqMedium)
                {
                    result.Warnings.Add($"Thiếu {reqMedium - bankMedium} câu hỏi Trung bình (Yêu cầu: {reqMedium}, Ngân hàng có: {bankMedium})");
                    result.CanGenerate = false;
                }
                if (bankHard < reqHard)
                {
                    result.Warnings.Add($"Thiếu {reqHard - bankHard} câu hỏi Khó (Yêu cầu: {reqHard}, Ngân hàng có: {bankHard})");
                    result.CanGenerate = false;
                }
                if (bankMC < reqMC)
                {
                    result.Warnings.Add($"Thiếu {reqMC - bankMC} câu hỏi Trắc nghiệm (Yêu cầu: {reqMC}, Ngân hàng có: {bankMC})");
                    result.CanGenerate = false;
                }
                if (bankEssay < reqEssay)
                {
                    result.Warnings.Add($"Thiếu {reqEssay - bankEssay} câu hỏi Tự luận (Yêu cầu: {reqEssay}, Ngân hàng có: {bankEssay})");
                    result.CanGenerate = false;
                }

                // If global targets are satisfied, verify cell distribution
                if (result.CanGenerate)
                {
                    bool canDistribute = false;
                    for (int ee = 0; ee <= config.SoCauEssay; ee++)
                    {
                        for (int me = 0; me <= config.SoCauEssay - ee; me++)
                        {
                            int he = config.SoCauEssay - ee - me;
                            int em = config.SoCauEasy - ee;
                            int mm = config.SoCauMedium - me;
                            int hm = config.SoCauHard - he;

                            if (em >= 0 && mm >= 0 && hm >= 0 && (em + mm + hm == config.SoCauMC))
                            {
                                if (config.SoLuongDe * ee <= bankEasyEssay &&
                                    config.SoLuongDe * me <= bankMediumEssay &&
                                    config.SoLuongDe * he <= bankHardEssay &&
                                    config.SoLuongDe * em <= bankEasyMC &&
                                    config.SoLuongDe * mm <= bankMediumMC &&
                                    config.SoLuongDe * hm <= bankHardMC)
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
                        result.Warnings.Add("Không thể phân bổ câu hỏi vừa khớp giữa các thể loại và độ khó từ ngân hàng câu hỏi.");
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

        public async Task<BaseResponseDto> GenerateExamsForKyThiAsync(int kyThiId, ExamGenerationConfigDto config, int createdBy)
        {
            try
            {
                var examCampaign = await _context.Set<ExamCampaign>().FindAsync(kyThiId);
                if (examCampaign == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (examCampaign.TotalQuestions.HasValue && config.TotalQuestions != examCampaign.TotalQuestions.Value)
                    return new BaseResponseDto { Success = false, Message = $"Số lượng câu hỏi của đề thi ({config.TotalQuestions}) không khớp với tổng số câu hỏi đã thiết lập cho kỳ thi ({examCampaign.TotalQuestions.Value})" };

                string? targetKhoaPhong = config.Department;
                if (!string.IsNullOrEmpty(targetKhoaPhong) && 
                    (targetKhoaPhong.Trim() == "Tất cả các khoa" || targetKhoaPhong.Trim() == "Tất cả khoa phòng"))
                {
                    targetKhoaPhong = null;
                }

                // 1. Run check
                var originalKhoaPhong = config.Department;
                config.Department = targetKhoaPhong;
                var checkRes = await CheckExamsAvailabilityAsync(kyThiId, config);
                if (!checkRes.Success || checkRes.Data == null || !checkRes.Data.CanGenerate)
                {
                    var errors = checkRes.Data?.Warnings ?? new List<string> { "Không đủ câu hỏi trong ngân hàng" };
                    return new BaseResponseDto { Success = false, Message = "Kiểm tra ngân hàng câu hỏi không đạt yêu cầu", Errors = errors };
                }

                // 2. Resolve cells
                int targetEE = 0, targetME = 0, targetHE = 0;
                int targetEM = 0, targetMM = 0, targetHM = 0;
                bool solved = false;

                for (int ee = 0; ee <= config.SoCauEssay; ee++)
                {
                    for (int me = 0; me <= config.SoCauEssay - ee; me++)
                    {
                        int he = config.SoCauEssay - ee - me;
                        int em = config.SoCauEasy - ee;
                        int mm = config.SoCauMedium - me;
                        int hm = config.SoCauHard - he;

                        if (em >= 0 && mm >= 0 && hm >= 0 && (em + mm + hm == config.SoCauMC))
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
                    .Include(c => c.IdLoaiCauHoiNavigation)
                    .Where(q => q.DaXoa != true);

                if (!string.IsNullOrEmpty(targetKhoaPhong))
                    query = query.Where(q => q.Department == targetKhoaPhong);

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.Content?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                bool IsEssay(Question q) => q.QuestionCategoryId == 3 || (q.IdLoaiCauHoiNavigation != null && q.IdLoaiCauHoiNavigation.CategoryName != null && q.IdLoaiCauHoiNavigation.CategoryName.ToLower().Contains("tự luận"));
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
                    for (int i = 0; i < config.SoLuongDe; i++)
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
                            Department = targetKhoaPhong,
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy,
                            KyThiId = examCampaign.Id,
                            LinkTruyCap = $"/exam/{examPaperCode}",
                            TotalScore = config.TotalQuestions,
                            IsResultPublished = false
                        };

                        _context.ExamPapers.Add(examPaper);
                        await _context.SaveChangesAsync();

                        // Select questions for this exam
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

                    _logger.LogInformation("Successfully generated {Count} non-overlapping exams for ExamCampaign {KyThiId}", config.SoLuongDe, kyThiId);
                    return new BaseResponseDto { Success = true, Message = $"Đã tạo thành công {config.SoLuongDe} đề thi không trùng lặp cho kỳ thi." };
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error generating exams for ExamCampaign {KyThiId}", kyThiId);
                    return new BaseResponseDto { Success = false, Message = "Lỗi cơ sở dữ liệu khi tạo bộ đề thi." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exams for ExamCampaign {KyThiId}", kyThiId);
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }
    }
}
