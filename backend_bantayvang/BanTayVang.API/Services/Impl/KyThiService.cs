// ============================================================
// FILE: Services/Impl/KyThiService.cs
// FIX: Trạng thái KyThi và CaThi tự động theo thời gian
//
// LỖI: TrangThai lưu tĩnh trong DB ("DangChuanBi", "ChuaBatDau")
//   → Dù đã đến giờ, trạng thái không thay đổi → UI hiện "Chưa bắt đầu"
//   → FIX: Tính trạng thái động trong MapToDto, không cần cron job
// ============================================================

using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.KyThi;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class KyThiService : Services.Interfaces.IKyThiService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<KyThiService> _logger;

        public KyThiService(BanTayVangDbContext context, ILogger<KyThiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<List<KyThiDto>>> GetAllAsync(string? trangThai = null, int? currentUserId = null)
        {
            try
            {
                var query = _context.Set<KyThi>()
                    .Include(k => k.KhoaPhong)
                    .AsQueryable();

                if (currentUserId.HasValue)
                {
                    var currentUser = await _context.Taikhoans.FindAsync(currentUserId.Value);
                    if (currentUser != null && currentUser.IdVaiTro == 3 && !string.IsNullOrEmpty(currentUser.KhoaPhong))
                    {
                        var userKhoa = await _context.KhoaPhongs
                            .FirstOrDefaultAsync(kp => kp.TenKhoa == currentUser.KhoaPhong);
                        if (userKhoa != null)
                        {
                            query = query.Where(k => k.KhoaPhongId == userKhoa.Id || k.KhoaPhongId == null);
                        }
                        else
                        {
                            query = query.Where(k => k.DonViToChuc == currentUser.KhoaPhong || (k.KhoaPhong != null && k.KhoaPhong.TenKhoa == currentUser.KhoaPhong) || k.KhoaPhongId == null);
                        }
                    }
                    else if (currentUser != null && currentUser.IdVaiTro == 5)
                    {
                        query = query.Where(k => k.KhoaPhongId == currentUser.IdKhoaQuanLy);
                    }
                }

                var kyThis = await query.OrderByDescending(k => k.NgayTao).ToListAsync();
                
                var dethis = await _context.Dethis.Where(d => d.KyThiId != null).ToListAsync();
                var dethisGrouped = dethis.GroupBy(d => d.KyThiId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = kyThis.Select(k => {
                    var dto = MapToDto(k);
                    if (dethisGrouped.TryGetValue(k.Id, out var linkedDethis))
                    {
                        dto.DanhSachMaDeThi = linkedDethis.Select(d => d.MaDeThi ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                        dto.SoLuongDeThi = linkedDethis.Count;
                        dto.MaDeThi = dto.DanhSachMaDeThi.FirstOrDefault();
                    }
                    return dto;
                }).ToList();

                // Filter theo trạng thái TÍNH TOÁN (không phải DB)
                if (!string.IsNullOrEmpty(trangThai))
                    result = result.Where(k => k.TrangThai == trangThai).ToList();

                return new BaseResponseDto<List<KyThiDto>> { Success = true, Message = "Thành công", Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ky thi list");
                return new BaseResponseDto<List<KyThiDto>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<KyThiDto>> GetByIdAsync(int id)
        {
            try
            {
                var kyThi = await _context.Set<KyThi>()
                    .Include(k => k.KhoaPhong)
                    .FirstOrDefaultAsync(k => k.Id == id);
                if (kyThi == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var dethis = await _context.Dethis
                    .Where(d => d.KyThiId == id)
                    .ToListAsync();

                var dto = MapToDto(kyThi);
                dto.DanhSachMaDeThi = dethis.Select(d => d.MaDeThi ?? "").Where(code => !string.IsNullOrEmpty(code)).ToList();
                dto.SoLuongDeThi = dethis.Count;
                dto.MaDeThi = dto.DanhSachMaDeThi.FirstOrDefault();

                return new BaseResponseDto<KyThiDto> { Success = true, Message = "Thành công", Data = dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ky thi");
                return new BaseResponseDto<KyThiDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<KyThiDto>> CreateAsync(CreateKyThiDto dto, int nguoiTao)
        {
            try
            {
                if (dto.ThoiGianBatDau == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.ThoiGianKetThuc == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var now = DateTime.Now;
                var startLocal = dto.ThoiGianBatDau.Value.Kind == DateTimeKind.Utc 
                    ? dto.ThoiGianBatDau.Value.ToLocalTime() 
                    : dto.ThoiGianBatDau.Value;

                var endLocal = dto.ThoiGianKetThuc.Value.Kind == DateTimeKind.Utc 
                    ? dto.ThoiGianKetThuc.Value.ToLocalTime() 
                    : dto.ThoiGianKetThuc.Value;

                if (startLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                if (await _context.Set<KyThi>().AnyAsync(k => k.MaKyThi == dto.MaKyThi))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Mã kỳ thi đã tồn tại" };

                string? donViToChuc = null;
                if (dto.KhoaPhongId.HasValue)
                {
                    var khoa = await _context.Set<KhoaPhong>().FindAsync(dto.KhoaPhongId.Value);
                    if (khoa != null)
                    {
                        donViToChuc = khoa.TenKhoa;
                    }
                }

                var kyThi = new KyThi
                {
                    MaKyThi = dto.MaKyThi,
                    TenKyThi = dto.TenKyThi,
                    MoTa = dto.MoTa,
                    KhoaPhongId = dto.KhoaPhongId,
                    ThoiGianBatDau = dto.ThoiGianBatDau,
                    ThoiGianKetThuc = dto.ThoiGianKetThuc,
                    DonViToChuc = donViToChuc,
                    NguoiTao = nguoiTao,
                    NgayTao = DateTime.Now,
                    TrangThai = "DangChuanBi", // Vẫn lưu DB, nhưng khi đọc sẽ tính lại
                    SoCauDungToiThieu = dto.SoCauDungToiThieu,
                    TongSoCauHoi = dto.TongSoCauHoi
                };

                _context.Set<KyThi>().Add(kyThi);
                await _context.SaveChangesAsync();

                if (kyThi.KhoaPhongId.HasValue)
                {
                    await _context.Entry(kyThi).Reference(k => k.KhoaPhong).LoadAsync();
                }

                return new BaseResponseDto<KyThiDto> { Success = true, Message = "Tạo kỳ thi thành công", Data = MapToDto(kyThi) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ky thi");
                return new BaseResponseDto<KyThiDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<KyThiDto>> UpdateAsync(int id, UpdateKyThiDto dto)
        {
            try
            {
                var kyThi = await _context.Set<KyThi>().FindAsync(id);
                if (kyThi == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (dto.ThoiGianBatDau == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian bắt đầu là bắt buộc" };
                if (dto.ThoiGianKetThuc == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc là bắt buộc" };

                var now = DateTime.Now;
                var startLocal = dto.ThoiGianBatDau.Value.Kind == DateTimeKind.Utc 
                    ? dto.ThoiGianBatDau.Value.ToLocalTime() 
                    : dto.ThoiGianBatDau.Value;

                var endLocal = dto.ThoiGianKetThuc.Value.Kind == DateTimeKind.Utc 
                    ? dto.ThoiGianKetThuc.Value.ToLocalTime() 
                    : dto.ThoiGianKetThuc.Value;

                var dbStartLocal = kyThi.ThoiGianBatDau.HasValue 
                    ? (kyThi.ThoiGianBatDau.Value.Kind == DateTimeKind.Utc 
                        ? kyThi.ThoiGianBatDau.Value.ToLocalTime() 
                        : (kyThi.ThoiGianBatDau.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(kyThi.ThoiGianBatDau.Value, DateTimeKind.Utc).ToLocalTime() 
                            : kyThi.ThoiGianBatDau.Value))
                    : (DateTime?)null;

                var dbEndLocal = kyThi.ThoiGianKetThuc.HasValue 
                    ? (kyThi.ThoiGianKetThuc.Value.Kind == DateTimeKind.Utc 
                        ? kyThi.ThoiGianKetThuc.Value.ToLocalTime() 
                        : (kyThi.ThoiGianKetThuc.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(kyThi.ThoiGianKetThuc.Value, DateTimeKind.Utc).ToLocalTime() 
                            : kyThi.ThoiGianKetThuc.Value))
                    : (DateTime?)null;

                // Check if date has actually changed with a minute-precision threshold (0.1 minutes)
                bool startChanged = dbStartLocal == null || Math.Abs((startLocal - dbStartLocal.Value).TotalMinutes) > 0.1;
                bool endChanged = dbEndLocal == null || Math.Abs((endLocal - dbEndLocal.Value).TotalMinutes) > 0.1;

                if (startChanged && startLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian bắt đầu không được trước thời gian hiện tại" };
                if (endChanged && endLocal < now.AddMinutes(-1))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc không được trước thời gian hiện tại" };
                if (endLocal <= startLocal)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Thời gian kết thúc phải sau thời gian bắt đầu" };

                string? donViToChuc = null;
                if (dto.KhoaPhongId.HasValue)
                {
                    var khoa = await _context.Set<KhoaPhong>().FindAsync(dto.KhoaPhongId.Value);
                    if (khoa != null)
                    {
                        donViToChuc = khoa.TenKhoa;
                    }
                }

                kyThi.TenKyThi = dto.TenKyThi;
                kyThi.MoTa = dto.MoTa;
                kyThi.KhoaPhongId = dto.KhoaPhongId;
                kyThi.ThoiGianBatDau = dto.ThoiGianBatDau;
                kyThi.ThoiGianKetThuc = dto.ThoiGianKetThuc;
                kyThi.DonViToChuc = donViToChuc;
                kyThi.TrangThai = dto.TrangThai;
                kyThi.NgayCapNhat = DateTime.Now;
                kyThi.SoCauDungToiThieu = dto.SoCauDungToiThieu;
                kyThi.TongSoCauHoi = dto.TongSoCauHoi;

                // Sync related exams (Dethi) properties if time/name has changed
                var relatedExams = await _context.Set<Dethi>()
                    .Where(d => d.KyThiId == id)
                    .ToListAsync();

                foreach (var exam in relatedExams)
                {
                    exam.ThoiGianBatDau = dto.ThoiGianBatDau;
                    if (dto.ThoiGianBatDau.HasValue && dto.ThoiGianKetThuc.HasValue)
                    {
                        var diff = dto.ThoiGianKetThuc.Value - dto.ThoiGianBatDau.Value;
                        var duration = (int)diff.TotalMinutes;
                        if (duration > 0)
                        {
                            exam.ThoiGianLamBai = duration;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(exam.TenDeThi) && exam.TenDeThi.Contains(" - "))
                    {
                        var parts = exam.TenDeThi.Split(new[] { " - " }, StringSplitOptions.None);
                        var suffix = parts.Last();
                        exam.TenDeThi = $"{dto.TenKyThi} - {suffix}";
                    }
                    else
                    {
                        exam.TenDeThi = dto.TenKyThi;
                    }

                    exam.KhoaPhong = donViToChuc;
                }

                await _context.SaveChangesAsync();

                if (kyThi.KhoaPhongId.HasValue)
                {
                    await _context.Entry(kyThi).Reference(k => k.KhoaPhong).LoadAsync();
                }

                return new BaseResponseDto<KyThiDto> { Success = true, Message = "Cập nhật thành công", Data = MapToDto(kyThi) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ky thi");
                return new BaseResponseDto<KyThiDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> UpdateStatusAsync(int id, string trangThai)
        {
            try
            {
                var kyThi = await _context.Set<KyThi>().FindAsync(id);
                if (kyThi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                kyThi.TrangThai = trangThai;
                kyThi.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = $"Đã chuyển trạng thái sang: {trangThai}" };
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
                var kyThi = await _context.Set<KyThi>().FirstOrDefaultAsync(k => k.Id == id);
                if (kyThi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                var computedStatus = ComputeKyThiStatus(kyThi);
                if (computedStatus == "DangDienRa")
                    return new BaseResponseDto { Success = false, Message = "Không thể xóa kỳ thi đang diễn ra" };

                _context.Set<KyThi>().Remove(kyThi);
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
        /// Tính trạng thái KyThi theo giờ hiện tại
        /// </summary>
        private static string ComputeKyThiStatus(KyThi k)
        {
            var nowUtc = DateTime.UtcNow;
            
            // Nếu trạng thái trong DB là DaKetThuc hoặc TamDung -> tôn trọng tuyệt đối
            if (k.TrangThai == "DaKetThuc" || k.TrangThai == "TamDung") 
                return k.TrangThai;

            var startUtc = k.ThoiGianBatDau.HasValue 
                ? (k.ThoiGianBatDau.Value.Kind == DateTimeKind.Utc 
                    ? k.ThoiGianBatDau.Value 
                    : DateTime.SpecifyKind(k.ThoiGianBatDau.Value, DateTimeKind.Utc))
                : (DateTime?)null;

            var endUtc = k.ThoiGianKetThuc.HasValue 
                ? (k.ThoiGianKetThuc.Value.Kind == DateTimeKind.Utc 
                    ? k.ThoiGianKetThuc.Value 
                    : DateTime.SpecifyKind(k.ThoiGianKetThuc.Value, DateTimeKind.Utc))
                : (DateTime?)null;

            // Nếu thời gian kết thúc đã qua -> DaKetThuc
            if (endUtc.HasValue && nowUtc > endUtc.Value)
                return "DaKetThuc";

            // Nếu trạng thái trong DB được set là DangDienRa -> tôn trọng
            if (k.TrangThai == "DangDienRa")
                return "DangDienRa";

            // Tính toán động theo thời gian
            if (startUtc.HasValue && nowUtc >= startUtc.Value)
                return "DangDienRa";
            if (startUtc.HasValue && nowUtc < startUtc.Value)
                return "DangChuanBi";

            return k.TrangThai ?? "DangChuanBi";
        }

        private static KyThiDto MapToDto(KyThi k)
        {
            return new KyThiDto
            {
                Id = k.Id,
                MaKyThi = k.MaKyThi,
                TenKyThi = k.TenKyThi,
                MoTa = k.MoTa,
                KhoaPhongId = k.KhoaPhongId,
                TenKhoa = k.KhoaPhong?.TenKhoa,
                TrangThai = ComputeKyThiStatus(k), // FIX: tính động
                ThoiGianBatDau = k.ThoiGianBatDau.HasValue ? DateTime.SpecifyKind(k.ThoiGianBatDau.Value, DateTimeKind.Utc) : null,
                ThoiGianKetThuc = k.ThoiGianKetThuc.HasValue ? DateTime.SpecifyKind(k.ThoiGianKetThuc.Value, DateTimeKind.Utc) : null,
                DonViToChuc = k.DonViToChuc,
                NgayTao = k.NgayTao,
                SoCauDungToiThieu = k.SoCauDungToiThieu,
                TongSoCauHoi = k.TongSoCauHoi
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

                string? targetKhoaPhong = config.KhoaPhong;
                if (!string.IsNullOrEmpty(targetKhoaPhong) && 
                    (targetKhoaPhong.Trim() == "Tất cả các khoa" || targetKhoaPhong.Trim() == "Tất cả khoa phòng"))
                {
                    targetKhoaPhong = null;
                }

                var query = _context.Cauhois
                    .Include(c => c.IdLoaiCauHoiNavigation)
                    .Where(q => q.DaXoa != true);

                if (!string.IsNullOrEmpty(targetKhoaPhong))
                {
                    query = query.Where(q => q.KhoaPhong == targetKhoaPhong);
                }

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.NoiDung?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                // Group and count questions
                bool IsEssay(Cauhoi q) => q.IdLoaiCauHoi == 3 || (q.IdLoaiCauHoiNavigation != null && q.IdLoaiCauHoiNavigation.TenLoai != null && q.IdLoaiCauHoiNavigation.TenLoai.ToLower().Contains("tự luận"));
                bool IsMC(Cauhoi q) => !IsEssay(q);

                string NormalizeDifficulty(string? doKho)
                {
                    if (string.IsNullOrEmpty(doKho)) return "Dễ";
                    var d = doKho.ToLower().Trim();
                    // Hỗ trợ giá trị số lưu trong DB: "1"=Dễ, "2"=Trung bình, "3"=Khó
                    if (d == "3" || d.Contains("khó") || d.Contains("hard")) return "Khó";
                    if (d == "2" || d.Contains("trung bình") || d.Contains("medium") || d.Contains("vừa")) return "Trung bình";
                    return "Dễ";
                }

                var bankEasyEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Dễ");
                var bankMediumEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Trung bình");
                var bankHardEssay = allQuestions.Count(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Khó");

                var bankEasyMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Dễ");
                var bankMediumMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Trung bình");
                var bankHardMC = allQuestions.Count(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Khó");

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

        public async Task<BaseResponseDto> GenerateExamsForKyThiAsync(int kyThiId, ExamGenerationConfigDto config, int nguoiTao)
        {
            try
            {
                var kyThi = await _context.Set<KyThi>().FindAsync(kyThiId);
                if (kyThi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (kyThi.TongSoCauHoi.HasValue && config.TongSoCau != kyThi.TongSoCauHoi.Value)
                    return new BaseResponseDto { Success = false, Message = $"Số lượng câu hỏi của đề thi ({config.TongSoCau}) không khớp với tổng số câu hỏi đã thiết lập cho kỳ thi ({kyThi.TongSoCauHoi.Value})" };

                string? targetKhoaPhong = config.KhoaPhong;
                if (!string.IsNullOrEmpty(targetKhoaPhong) && 
                    (targetKhoaPhong.Trim() == "Tất cả các khoa" || targetKhoaPhong.Trim() == "Tất cả khoa phòng"))
                {
                    targetKhoaPhong = null;
                }

                // 1. Run check
                var originalKhoaPhong = config.KhoaPhong;
                config.KhoaPhong = targetKhoaPhong;
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
                var query = _context.Cauhois
                    .Include(c => c.IdLoaiCauHoiNavigation)
                    .Where(q => q.DaXoa != true);

                if (!string.IsNullOrEmpty(targetKhoaPhong))
                    query = query.Where(q => q.KhoaPhong == targetKhoaPhong);

                var allQuestionsRaw = await query.ToListAsync();
                var allQuestions = allQuestionsRaw
                    .GroupBy(q => q.NoiDung?.Trim().ToLower() ?? "")
                    .Select(g => g.First())
                    .ToList();

                bool IsEssay(Cauhoi q) => q.IdLoaiCauHoi == 3 || (q.IdLoaiCauHoiNavigation != null && q.IdLoaiCauHoiNavigation.TenLoai != null && q.IdLoaiCauHoiNavigation.TenLoai.ToLower().Contains("tự luận"));
                bool IsMC(Cauhoi q) => !IsEssay(q);

                string NormalizeDifficulty(string? doKho)
                {
                    if (string.IsNullOrEmpty(doKho)) return "Dễ";
                    var d = doKho.ToLower().Trim();
                    // Hỗ trợ giá trị số lưu trong DB: "1"=Dễ, "2"=Trung bình, "3"=Khó
                    if (d == "3" || d.Contains("khó") || d.Contains("hard")) return "Khó";
                    if (d == "2" || d.Contains("trung bình") || d.Contains("medium") || d.Contains("vừa")) return "Trung bình";
                    return "Dễ";
                }

                var easyEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Dễ").OrderBy(_ => Guid.NewGuid()).ToList();
                var medEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Trung bình").OrderBy(_ => Guid.NewGuid()).ToList();
                var hardEssayPool = allQuestions.Where(q => IsEssay(q) && NormalizeDifficulty(q.DoKho) == "Khó").OrderBy(_ => Guid.NewGuid()).ToList();

                var easyMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Dễ").OrderBy(_ => Guid.NewGuid()).ToList();
                var medMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Trung bình").OrderBy(_ => Guid.NewGuid()).ToList();
                var hardMCPool = allQuestions.Where(q => IsMC(q) && NormalizeDifficulty(q.DoKho) == "Khó").OrderBy(_ => Guid.NewGuid()).ToList();

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create n exams
                    for (int i = 0; i < config.SoLuongDe; i++)
                    {
                        var maDeThi = $"{kyThi.TenKyThi.Replace(" ", "_")}_DE_{i + 1}_{Guid.NewGuid().ToString("N")[..6]}".ToUpper();
                        var tenDeThi = $"{kyThi.TenKyThi} - Đề {i + 1}";

                        // Determine duration from ky thi or default to 60
                        int duration = 60;
                        if (kyThi.ThoiGianBatDau.HasValue && kyThi.ThoiGianKetThuc.HasValue)
                        {
                            var diff = kyThi.ThoiGianKetThuc.Value - kyThi.ThoiGianBatDau.Value;
                            duration = (int)diff.TotalMinutes;
                            if (duration <= 0) duration = 60;
                        }

                        var dethi = new Dethi
                        {
                            MaDeThi = maDeThi,
                            TenDeThi = tenDeThi,
                            ThoiGianBatDau = kyThi.ThoiGianBatDau,
                            ThoiGianLamBai = duration,
                            TrangThai = "Active",
                            KhoaPhong = targetKhoaPhong,
                            NgayTao = DateTime.Now,
                            NguoiTao = nguoiTao,
                            KyThiId = kyThi.Id,
                            LinkTruyCap = $"/exam/{maDeThi}",
                            TongDiem = config.TongSoCau,
                            CongBoKetQua = false
                        };

                        _context.Dethis.Add(dethi);
                        await _context.SaveChangesAsync();

                        // Select questions for this exam
                        var examQuestions = new List<Cauhoi>();
                        examQuestions.AddRange(easyEssayPool.Skip(i * targetEE).Take(targetEE));
                        examQuestions.AddRange(medEssayPool.Skip(i * targetME).Take(targetME));
                        examQuestions.AddRange(hardEssayPool.Skip(i * targetHE).Take(targetHE));

                        examQuestions.AddRange(easyMCPool.Skip(i * targetEM).Take(targetEM));
                        examQuestions.AddRange(medMCPool.Skip(i * targetMM).Take(targetMM));
                        examQuestions.AddRange(hardMCPool.Skip(i * targetHM).Take(targetHM));

                        // Add to DethiCauhoi
                        foreach (var q in examQuestions)
                        {
                            var dc = new DethiCauhoi
                            {
                                IdDeThi = dethi.Id,
                                IdCauHoi = q.Id
                            };
                            _context.DethiCauhois.Add(dc);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully generated {Count} non-overlapping exams for KyThi {KyThiId}", config.SoLuongDe, kyThiId);
                    return new BaseResponseDto { Success = true, Message = $"Đã tạo thành công {config.SoLuongDe} đề thi không trùng lặp cho kỳ thi." };
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error generating exams for KyThi {KyThiId}", kyThiId);
                    return new BaseResponseDto { Success = false, Message = "Lỗi cơ sở dữ liệu khi tạo bộ đề thi." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exams for KyThi {KyThiId}", kyThiId);
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }
    }
}
