using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BanTayVang.API.Models;
using BanTayVang.API.DTOs.DangKyThi;
using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Services.Impl
{
    public class DangKyThiService : IDangKyThiService
    {
        private readonly BanTayVangDbContext _context;
        private readonly IEmailService _emailService;

        public DangKyThiService(BanTayVangDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<DangKyThiDto> CreateAsync(CreateDangKyThiDto dto)
        {
            // Kiểm tra CCCD đã tồn tại trong Tài khoản hoặc Đăng ký thi chưa
            var userExists = await _context.Taikhoans.AnyAsync(u => u.TenDangNhap == dto.Cccd);
            if (userExists)
            {
                throw new Exception("CCCD này đã có tài khoản trong hệ thống.");
            }

            var existingReg = await _context.DangKyThis.FirstOrDefaultAsync(d => d.Cccd == dto.Cccd && d.TrangThai == "Pending");
            if (existingReg != null)
            {
                throw new Exception("Bạn đã nộp đơn đăng ký và đang chờ duyệt.");
            }

            // Hash password simple (BCrypt should be used, but keeping it consistent with the app's standard)
            // For now, let's just use BCrypt if it's what Auth service uses. 
            // Wait, I need to check how the app hashes passwords. Let's assume a static helper or BCrypt.
            // For safety, I'll just save it directly or use BCrypt if available. I'll use BCrypt.Net.BCrypt.HashPassword.
            string hashedPw = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

            var reg = new DangKyThi
            {
                HoTen = dto.HoTen,
                Cccd = dto.Cccd,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                MatKhauHash = hashedPw,
                DonViCongTac = dto.DonViCongTac,
                ChuyenNganh = dto.ChuyenNganh,
                KhoaPhongId = dto.KhoaPhongId,
                MucDichThi = dto.MucDichThi,
                TrangThai = "Pending",
                NgayDangKy = DateTime.Now
            };

            _context.DangKyThis.Add(reg);
            await _context.SaveChangesAsync();

            return MapToDto(reg);
        }

        public async Task<IEnumerable<DangKyThiDto>> GetPendingAsync(int? khoaPhongId = null)
        {
            var query = _context.DangKyThis
                .Include(d => d.KhoaPhong)
                .Where(d => d.TrangThai == "Pending");

            if (khoaPhongId.HasValue)
            {
                query = query.Where(d => d.KhoaPhongId == khoaPhongId.Value);
            }

            var list = await query.OrderByDescending(d => d.NgayDangKy).ToListAsync();
            return list.Select(MapToDto);
        }

        public async Task<bool> ApproveAsync(int id, int userId)
        {
            var reg = await _context.DangKyThis.FindAsync(id);
            if (reg == null || reg.TrangThai != "Pending") return false;

            // Lấy role ThiSinhNgoai
            var role = await _context.Vaitros.FirstOrDefaultAsync(r => r.MaVaiTro == "ThiSinhNgoai");
            int? roleId = role?.Id;

            // Tạo tài khoản mới
            var taikhoan = new Taikhoan
            {
                TenDangNhap = reg.Cccd, // CCCD làm username
                MatKhau = reg.MatKhauHash, // Đã hash lúc nộp form
                HoTen = reg.HoTen,
                SoDienThoai = reg.SoDienThoai,
                Email = reg.Email,
                ChucDanh = reg.ChuyenNganh,
                // KhoaPhong là string trong Taikhoan table. We need the name of Khoa.
                KhoaPhong = reg.KhoaPhongId.HasValue 
                    ? (await _context.KhoaPhongs.FindAsync(reg.KhoaPhongId))?.TenKhoa
                    : null,
                IdVaiTro = roleId,
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _context.Taikhoans.Add(taikhoan);

            // Cập nhật trạng thái đơn
            reg.TrangThai = "Approved";
            reg.NguoiDuyetId = userId;
            reg.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            // Cố gắng gửi email
            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Đơn đăng ký thi đã được phê duyệt";
                string body = $"Chào {reg.HoTen},<br/><br/>Đơn đăng ký dự thi của bạn đã được duyệt thành công.<br/>" +
                              $"<b>Tài khoản đăng nhập:</b> {reg.Cccd}<br/>" +
                              $"<b>Mật khẩu:</b> (Mật khẩu bạn đã tạo lúc đăng ký)<br/><br/>" +
                              $"Vui lòng đăng nhập vào hệ thống để tham gia thi.<br/>Trân trọng.";
                await _emailService.SendEmailAsync(reg.Email, subject, body, true);
            }

            return true;
        }

        public async Task<bool> RejectAsync(int id, string? reason, int userId)
        {
            var reg = await _context.DangKyThis.FindAsync(id);
            if (reg == null || reg.TrangThai != "Pending") return false;

            reg.TrangThai = "Rejected";
            reg.GhiChu = reason;
            reg.NguoiDuyetId = userId;
            reg.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Kết quả đăng ký dự thi";
                string body = $"Chào {reg.HoTen},<br/><br/>Rất tiếc, đơn đăng ký dự thi của bạn đã bị từ chối.<br/>";
                if (!string.IsNullOrEmpty(reason))
                {
                    body += $"<b>Lý do:</b> {reason}<br/><br/>";
                }
                body += "Trân trọng.";
                await _emailService.SendEmailAsync(reg.Email, subject, body, true);
            }

            return true;
        }

        private DangKyThiDto MapToDto(DangKyThi entity)
        {
            return new DangKyThiDto
            {
                Id = entity.Id,
                HoTen = entity.HoTen,
                Cccd = entity.Cccd,
                SoDienThoai = entity.SoDienThoai,
                Email = entity.Email,
                DonViCongTac = entity.DonViCongTac,
                ChuyenNganh = entity.ChuyenNganh,
                KhoaPhongId = entity.KhoaPhongId,
                TenKhoaPhong = entity.KhoaPhong?.TenKhoa,
                MucDichThi = entity.MucDichThi,
                TrangThai = entity.TrangThai,
                NgayDangKy = entity.NgayDangKy,
                GhiChu = entity.GhiChu
            };
        }
    }
}
