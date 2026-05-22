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

        public async Task<BaseResponseDto<List<KyThiDto>>> GetAllAsync(string? trangThai = null)
        {
            try
            {
                var query = _context.Set<KyThi>().Include(k => k.CaThis).AsQueryable();
                if (!string.IsNullOrEmpty(trangThai))
                    query = query.Where(k => k.TrangThai == trangThai);

                var kyThis = await query.OrderByDescending(k => k.NgayTao).ToListAsync();
                var result = kyThis.Select(k => MapToDto(k)).ToList();

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
                var kyThi = await _context.Set<KyThi>().Include(k => k.CaThis).ThenInclude(c => c.DeThi)
                    .FirstOrDefaultAsync(k => k.Id == id);
                if (kyThi == null)
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                return new BaseResponseDto<KyThiDto> { Success = true, Message = "Thành công", Data = MapToDto(kyThi) };
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
                if (await _context.Set<KyThi>().AnyAsync(k => k.MaKyThi == dto.MaKyThi))
                    return new BaseResponseDto<KyThiDto> { Success = false, Message = "Mã kỳ thi đã tồn tại" };

                var kyThi = new KyThi
                {
                    MaKyThi = dto.MaKyThi,
                    TenKyThi = dto.TenKyThi,
                    MoTa = dto.MoTa,
                    LoaiKyThi = dto.LoaiKyThi,
                    ThoiGianBatDau = dto.ThoiGianBatDau,
                    ThoiGianKetThuc = dto.ThoiGianKetThuc,
                    DonViToChuc = dto.DonViToChuc,
                    NguoiTao = nguoiTao,
                    NgayTao = DateTime.Now,
                    TrangThai = "DangChuanBi"
                };

                _context.Set<KyThi>().Add(kyThi);
                await _context.SaveChangesAsync();

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

                kyThi.TenKyThi = dto.TenKyThi;
                kyThi.MoTa = dto.MoTa;
                kyThi.LoaiKyThi = dto.LoaiKyThi;
                kyThi.ThoiGianBatDau = dto.ThoiGianBatDau;
                kyThi.ThoiGianKetThuc = dto.ThoiGianKetThuc;
                kyThi.DonViToChuc = dto.DonViToChuc;
                kyThi.TrangThai = dto.TrangThai;
                kyThi.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();
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
                kyThi.NgayCapNhat = DateTime.Now;
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
                var kyThi = await _context.Set<KyThi>().Include(k => k.CaThis).FirstOrDefaultAsync(k => k.Id == id);
                if (kyThi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy kỳ thi" };

                if (kyThi.TrangThai == "DangDienRa")
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

        public async Task<BaseResponseDto<List<CaThiDto>>> GetCaThiByKyThiAsync(int kyThiId)
        {
            try
            {
                var caThis = await _context.Set<CaThi>()
                    .Where(c => c.KyThiId == kyThiId)
                    .Include(c => c.DeThi)
                    .OrderBy(c => c.ThoiGianBatDau)
                    .ToListAsync();

                var result = caThis.Select(c => new CaThiDto
                {
                    Id = c.Id,
                    KyThiId = c.KyThiId,
                    DeThiId = c.DeThiId,
                    MaDeThi = c.DeThi?.MaDeThi,
                    TenCa = c.TenCa,
                    ThoiGianBatDau = c.ThoiGianBatDau,
                    ThoiGianKetThuc = c.ThoiGianKetThuc,
                    SoLuongToiDa = c.SoLuongToiDa,
                    TrangThai = c.TrangThai,
                    GhiChu = c.GhiChu
                }).ToList();

                return new BaseResponseDto<List<CaThiDto>> { Success = true, Message = "Thành công", Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ca thi");
                return new BaseResponseDto<List<CaThiDto>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<CaThiDto>> CreateCaThiAsync(CreateCaThiDto dto)
        {
            try
            {
                var kyThi = await _context.Set<KyThi>().FindAsync(dto.KyThiId);
                if (kyThi == null)
                    return new BaseResponseDto<CaThiDto> { Success = false, Message = "Không tìm thấy kỳ thi" };

                var caThi = new CaThi
                {
                    KyThiId = dto.KyThiId,
                    DeThiId = dto.DeThiId,
                    TenCa = dto.TenCa,
                    ThoiGianBatDau = dto.ThoiGianBatDau,
                    ThoiGianKetThuc = dto.ThoiGianKetThuc,
                    SoLuongToiDa = dto.SoLuongToiDa,
                    GhiChu = dto.GhiChu,
                    TrangThai = "ChuaBatDau"
                };

                _context.Set<CaThi>().Add(caThi);
                await _context.SaveChangesAsync();

                return new BaseResponseDto<CaThiDto>
                {
                    Success = true,
                    Message = "Tạo ca thi thành công",
                    Data = new CaThiDto
                    {
                        Id = caThi.Id,
                        KyThiId = caThi.KyThiId,
                        DeThiId = caThi.DeThiId,
                        TenCa = caThi.TenCa,
                        ThoiGianBatDau = caThi.ThoiGianBatDau,
                        ThoiGianKetThuc = caThi.ThoiGianKetThuc,
                        SoLuongToiDa = caThi.SoLuongToiDa,
                        TrangThai = caThi.TrangThai,
                        GhiChu = caThi.GhiChu
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ca thi");
                return new BaseResponseDto<CaThiDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<CaThiDto>> UpdateCaThiAsync(int id, CreateCaThiDto dto)
        {
            try
            {
                var caThi = await _context.Set<CaThi>().FindAsync(id);
                if (caThi == null)
                    return new BaseResponseDto<CaThiDto> { Success = false, Message = "Không tìm thấy ca thi" };

                caThi.DeThiId = dto.DeThiId;
                caThi.TenCa = dto.TenCa;
                caThi.ThoiGianBatDau = dto.ThoiGianBatDau;
                caThi.ThoiGianKetThuc = dto.ThoiGianKetThuc;
                caThi.SoLuongToiDa = dto.SoLuongToiDa;
                caThi.GhiChu = dto.GhiChu;

                await _context.SaveChangesAsync();
                return new BaseResponseDto<CaThiDto> { Success = true, Message = "Cập nhật thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ca thi");
                return new BaseResponseDto<CaThiDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto> DeleteCaThiAsync(int id)
        {
            try
            {
                var caThi = await _context.Set<CaThi>().FindAsync(id);
                if (caThi == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy ca thi" };

                _context.Set<CaThi>().Remove(caThi);
                await _context.SaveChangesAsync();
                return new BaseResponseDto { Success = true, Message = "Xóa ca thi thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ca thi");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        private static KyThiDto MapToDto(KyThi k)
        {
            return new KyThiDto
            {
                Id = k.Id,
                MaKyThi = k.MaKyThi,
                TenKyThi = k.TenKyThi,
                MoTa = k.MoTa,
                LoaiKyThi = k.LoaiKyThi,
                TrangThai = k.TrangThai,
                ThoiGianBatDau = k.ThoiGianBatDau,
                ThoiGianKetThuc = k.ThoiGianKetThuc,
                DonViToChuc = k.DonViToChuc,
                NgayTao = k.NgayTao,
                SoCaThi = k.CaThis.Count,
                DanhSachCaThi = k.CaThis.Select(c => new CaThiDto
                {
                    Id = c.Id,
                    KyThiId = c.KyThiId,
                    DeThiId = c.DeThiId,
                    MaDeThi = c.DeThi?.MaDeThi,
                    TenCa = c.TenCa,
                    ThoiGianBatDau = c.ThoiGianBatDau,
                    ThoiGianKetThuc = c.ThoiGianKetThuc,
                    SoLuongToiDa = c.SoLuongToiDa,
                    TrangThai = c.TrangThai,
                    GhiChu = c.GhiChu
                }).ToList()
            };
        }
    }
}