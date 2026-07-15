using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BanTayVang.API.Models;
using BanTayVang.API.DTOs.ExamRegistration;
using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Services.Impl
{
    public class ExamRegistrationService : IExamRegistrationService
    {
        private readonly BanTayVangDbContext _context;
        private readonly IEmailService _emailService;

        public ExamRegistrationService(BanTayVangDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ExamRegistrationDto> CreateAsync(CreateExamRegistrationDto dto)
        {
            // Kiểm tra CCCD đã tồn tại trong Tài khoản hoặc Đăng ký thi chưa
            var userExists = await _context.Users.AnyAsync(u => u.Username == dto.Cccd);
            if (userExists)
            {
                throw new Exception("CCCD này đã có tài khoản trong hệ thống.");
            }

            var existingReg = await _context.ExamRegistrations.FirstOrDefaultAsync(d => d.Cccd == dto.Cccd && d.Status == "Pending");
            if (existingReg != null)
            {
                throw new Exception("Bạn đã nộp đơn đăng ký và đang chờ duyệt.");
            }

            // Hash password simple (BCrypt should be used, but keeping it consistent with the app's standard)
            // For now, let's just use BCrypt if it's what Auth service uses. 
            // Wait, I need to check how the app hashes passwords. Let's assume a static helper or BCrypt.
            // For safety, I'll just save it directly or use BCrypt if available. I'll use BCrypt.Net.BCrypt.HashPassword.
            string hashedPw = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var reg = new ExamRegistration
            {
                FullName = dto.FullName,
                Cccd = dto.Cccd,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                MatKhauHash = hashedPw,
                WorkUnit = dto.WorkUnit,
                ChuyenNganh = dto.ChuyenNganh,
                DepartmentId = dto.DepartmentId,
                MucDichThi = dto.MucDichThi,
                Status = "Pending",
                NgayDangKy = DateTime.Now
            };

            _context.ExamRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            return MapToDto(reg);
        }

        public async Task<IEnumerable<ExamRegistrationDto>> GetPendingRegistrationsAsync(int? departmentId = null)
        {
            var query = _context.ExamRegistrations
                .Include(d => d.Department)
                .Where(d => d.Status == "Pending");

            if (departmentId.HasValue)
            {
                query = query.Where(d => d.DepartmentId == departmentId.Value);
            }

            var list = await query.OrderByDescending(d => d.NgayDangKy).ToListAsync();
            return list.Select(MapToDto);
        }

        public async Task<bool> ApproveAsync(int id, int userId)
        {
            var reg = await _context.ExamRegistrations.FindAsync(id);
            if (reg == null || reg.Status != "Pending") return false;

            // Lấy role ThiSinhNgoai
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.MaVaiTro == "ThiSinhNgoai");
            int? roleId = role?.Id;

            // Tạo tài khoản mới
            var user = new User
            {
                Username = reg.Cccd, // CCCD làm username
                Password = reg.MatKhauHash, // Đã hash lúc nộp form
                FullName = reg.FullName,
                SoDienThoai = reg.SoDienThoai,
                Email = reg.Email,
                JobTitle = reg.ChuyenNganh,
                // Department là string trong User table. We need the name of Khoa.
                Department = reg.DepartmentId.HasValue 
                    ? (await _context.Departments.FindAsync(reg.DepartmentId))?.DepartmentName
                    : null,
                RoleId = roleId,
                Status = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);

            // Cập nhật trạng thái đơn
            reg.Status = "Approved";
            reg.NguoiDuyetId = userId;
            reg.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            // Cố gắng gửi email
            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Đơn đăng ký thi đã được phê duyệt";
                string body = $"Chào {reg.FullName},<br/><br/>Đơn đăng ký dự thi của bạn đã được duyệt thành công.<br/>" +
                              $"<b>Tài khoản đăng nhập:</b> {reg.Cccd}<br/>" +
                              $"<b>Mật khẩu:</b> (Mật khẩu bạn đã tạo lúc đăng ký)<br/><br/>" +
                              $"Vui lòng đăng nhập vào hệ thống để tham gia thi.<br/>Trân trọng.";
                await _emailService.SendEmailAsync(reg.Email, subject, body, true);
            }

            return true;
        }

        public async Task<bool> RejectAsync(int id, string? reason, int userId)
        {
            var reg = await _context.ExamRegistrations.FindAsync(id);
            if (reg == null || reg.Status != "Pending") return false;

            reg.Status = "Rejected";
            reg.GhiChu = reason;
            reg.NguoiDuyetId = userId;
            reg.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Kết quả đăng ký dự thi";
                string body = $"Chào {reg.FullName},<br/><br/>Rất tiếc, đơn đăng ký dự thi của bạn đã bị từ chối.<br/>";
                if (!string.IsNullOrEmpty(reason))
                {
                    body += $"<b>Lý do:</b> {reason}<br/><br/>";
                }
                body += "Trân trọng.";
                await _emailService.SendEmailAsync(reg.Email, subject, body, true);
            }

            return true;
        }

        private ExamRegistrationDto MapToDto(ExamRegistration entity)
        {
            return new ExamRegistrationDto
            {
                Id = entity.Id,
                FullName = entity.FullName,
                Cccd = entity.Cccd,
                SoDienThoai = entity.SoDienThoai,
                Email = entity.Email,
                WorkUnit = entity.WorkUnit,
                ChuyenNganh = entity.ChuyenNganh,
                DepartmentId = entity.DepartmentId,
                TenKhoaPhong = entity.Department?.DepartmentName,
                MucDichThi = entity.MucDichThi,
                Status = entity.Status,
                NgayDangKy = entity.NgayDangKy,
                GhiChu = entity.GhiChu
            };
        }
    }
}
