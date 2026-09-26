using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BanTayVang.API.Models;
using BanTayVang.API.DTOs.ExamRegistration;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;

namespace BanTayVang.API.Services.Impl
{
    public class ExamRegistrationService : IExamRegistrationService
    {
        private readonly BanTayVangDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordService _passwordService;

        public ExamRegistrationService(
            BanTayVangDbContext context,
            IEmailService emailService,
            IEmailVerificationService emailVerificationService,
            IPasswordService passwordService)
        {
            _context = context;
            _emailService = emailService;
            _emailVerificationService = emailVerificationService;
            _passwordService = passwordService;
        }

        public async Task<ExamRegistrationDto?> CreateAsync(CreateExamRegistrationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new Exception("Vui lòng cung cấp email.");
            }

            // BUG FIX: CCCD/phone were never validated for shape, and were compared/stored
            // as-is - "012345678901" and "012345678901 " (trailing space) or mixed case (for the
            // rare alphanumeric legacy CMND) were treated as different values, defeating the very
            // duplicate-CCCD checks below and letting the same person re-register under a
            // "different" CCCD that only differs by whitespace. Trim + validate shape up front.
            var idCardNumber = dto.IdCardNumber?.Trim() ?? string.Empty;
            if (!System.Text.RegularExpressions.Regex.IsMatch(idCardNumber, @"^\d{9}(\d{3})?$"))
            {
                throw new Exception("Số CCCD/CMND không hợp lệ (phải là 9 hoặc 12 chữ số).");
            }
            var phoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^0\d{9,10}$"))
            {
                throw new Exception("Số điện thoại không hợp lệ.");
            }

            // BUG FIX: [Required] on the DTO only rejects a null DepartmentId - a caller bypassing
            // the form (or a stale FE build) could still send an ID that doesn't exist in
            // Departments, which would silently become a null Department on the User once
            // approved (same "sees every campaign" gap this fix is closing).
            var departmentExists = await _context.Departments.AnyAsync(k => k.Id == dto.DepartmentId!.Value);
            if (!departmentExists)
            {
                throw new Exception("Khoa/phòng ban không hợp lệ.");
            }

            // BUG FIX (PII disclosure): this used to throw a distinct, specific message for
            // "CCCD already has an account" vs "CCCD already has a pending application" - both
            // returned straight to an ANONYMOUS, unauthenticated caller. That let anyone probe any
            // CCCD number (a national ID, not something the caller need prove ownership of) and
            // learn whether that real person already has an account or a pending request in this
            // hospital's internal system, with zero authentication. Now both cases return null
            // (silently not creating a duplicate row) and the controller sends back the exact same
            // response shape/wording as a genuine new submission, so probing is indistinguishable
            // from a real success.
            var userExists = await _context.Users.AnyAsync(u => u.Username == idCardNumber);
            if (userExists)
            {
                return null;
            }

            var existingReg = await _context.ExamRegistrations.FirstOrDefaultAsync(d => d.IdCardNumber == idCardNumber && d.Status == "Pending");
            if (existingReg != null)
            {
                return null;
            }

            // BUG FIX: this had no password strength check at all (only the DTO's [Required]) -
            // confirmed live a 1-character password was accepted here, and this account becomes a
            // real login-capable account the moment an admin approves the registration. Reuse the
            // same ValidatePasswordStrength rules enforced on every other account-creation path.
            var passwordValidation = _passwordService.ValidatePasswordStrength(dto.Password);
            if (!passwordValidation.IsValid)
            {
                throw new Exception(string.Join(" ", passwordValidation.Errors));
            }

            string hashedPw = _passwordService.HashPassword(dto.Password);

            var reg = new ExamRegistration
            {
                FullName = dto.FullName,
                IdCardNumber = idCardNumber,
                PhoneNumber = phoneNumber,
                Email = dto.Email,
                PasswordHash = hashedPw,
                WorkUnit = dto.WorkUnit,
                Major = dto.Major,
                DepartmentId = dto.DepartmentId,
                ExamPurpose = dto.ExamPurpose,
                Status = "Pending",
                RegistrationDate = DateTime.UtcNow.AddHours(7)
            };

            _context.ExamRegistrations.Add(reg);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // BUG FIX: the check-then-act reads above have a race window (two concurrent
                // submissions for the same CCCD can both pass the checks before either commits) -
                // the UX_ExamRegistrations_IdCardNumber_Pending / UX_Users_Username unique indexes
                // (see Program.cs) correctly stop the DB from ever storing the duplicate, but
                // without this catch the loser of the race would bubble up as a raw, untranslated
                // EF/SQL exception message instead of the same "already registered" outcome.
                return null;
            }

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

            var list = await query.OrderByDescending(d => d.RegistrationDate).ToListAsync();
            return list.Select(MapToDto);
        }

        public async Task<bool> ApproveAsync(int id, int userId)
        {
            var reg = await _context.ExamRegistrations.FindAsync(id);
            if (reg == null || reg.Status != "Pending") return false;

            // Lấy role ThiSinhNgoai
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ThiSinhNgoai");
            int? roleId = role?.Id;

            // Tạo tài khoản mới
            var user = new User
            {
                Username = reg.IdCardNumber, // CCCD làm username
                Password = reg.PasswordHash, // Đã hash lúc nộp form
                FullName = reg.FullName,
                PhoneNumber = reg.PhoneNumber,
                Email = reg.Email,
                JobTitle = reg.Major,
                // Department là string trong User table. We need the name of Khoa.
                Department = reg.DepartmentId.HasValue 
                    ? (await _context.Departments.FindAsync(reg.DepartmentId))?.DepartmentName
                    : null,
                RoleId = roleId,
                Status = true,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            _context.Users.Add(user);

            // Cập nhật trạng thái đơn
            reg.Status = "Approved";
            reg.ApproverId = userId;
            reg.ApprovalDate = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            // Cố gắng gửi email
            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Đơn đăng ký thi đã được phê duyệt";
                // BUG FIX: FullName comes straight from the public, unauthenticated registration
                // form and was interpolated into an isHtml:true email body unescaped - anyone
                // could submit someone else's real address as reg.Email (no email verification
                // exists for this flow) plus HTML/script in FullName, turning our own
                // "no-reply@bantayvang.vn" sender into an HTML-injection/phishing delivery vector.
                string safeFullName = WebUtility.HtmlEncode(reg.FullName);
                string body = $"Chào {safeFullName},<br/><br/>Đơn đăng ký dự thi của bạn đã được duyệt thành công.<br/>" +
                              $"<b>Tài khoản đăng nhập:</b> {reg.IdCardNumber}<br/>" +
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
            reg.Notes = reason;
            reg.ApproverId = userId;
            reg.ApprovalDate = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(reg.Email))
            {
                string subject = "Kết quả đăng ký dự thi";
                string safeFullName = WebUtility.HtmlEncode(reg.FullName);
                string body = $"Chào {safeFullName},<br/><br/>Rất tiếc, đơn đăng ký dự thi của bạn đã bị từ chối.<br/>";
                if (!string.IsNullOrEmpty(reason))
                {
                    body += $"<b>Lý do:</b> {WebUtility.HtmlEncode(reason)}<br/><br/>";
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
                IdCardNumber = entity.IdCardNumber,
                PhoneNumber = entity.PhoneNumber,
                Email = entity.Email,
                WorkUnit = entity.WorkUnit,
                Major = entity.Major,
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department?.DepartmentName,
                ExamPurpose = entity.ExamPurpose,
                Status = entity.Status,
                RegistrationDate = entity.RegistrationDate,
                Notes = entity.Notes
            };
        }
    }
}

