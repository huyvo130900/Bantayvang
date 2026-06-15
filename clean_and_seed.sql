USE HeThongBanTayVang;
GO

PRINT '========================================';
PRINT 'CLEANING ALL DATA IN DATABASE';
PRINT '========================================';

-- Disable all foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";
PRINT '  [OK] Disabled all foreign key constraints';

-- Delete data from all tables using DELETE FROM to avoid FK truncate errors
DELETE FROM PhucHuy.RefreshTokens;
DBCC CHECKIDENT ('PhucHuy.RefreshTokens', RESEED, 0);

DELETE FROM PhucHuy.UserSessions;
DBCC CHECKIDENT ('PhucHuy.UserSessions', RESEED, 0);
PRINT '  [OK] Cleared RefreshTokens and UserSessions';

DELETE FROM Notifications;
DBCC CHECKIDENT ('Notifications', RESEED, 0);

IF EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    DELETE FROM AuditLogs;
    DBCC CHECKIDENT ('AuditLogs', RESEED, 0);
END

DELETE FROM LOGTHAOTAC;
DBCC CHECKIDENT ('LOGTHAOTAC', RESEED, 0);
PRINT '  [OK] Cleared Notifications, AuditLogs, LOGTHAOTAC';

DELETE FROM ExamAssignments;
DBCC CHECKIDENT ('ExamAssignments', RESEED, 0);

DELETE FROM CANHBAOGIANLAN;
DBCC CHECKIDENT ('CANHBAOGIANLAN', RESEED, 0);

DELETE FROM CHITIETLAMBAI;
DBCC CHECKIDENT ('CHITIETLAMBAI', RESEED, 0);

DELETE FROM BAITHI;
DBCC CHECKIDENT ('BAITHI', RESEED, 0);
PRINT '  [OK] Cleared ExamAssignments, CANHBAOGIANLAN, CHITIETLAMBAI, BAITHI';

DELETE FROM DETHI_CAUHOI;
DBCC CHECKIDENT ('DETHI_CAUHOI', RESEED, 0);

DELETE FROM DETHI;
DBCC CHECKIDENT ('DETHI', RESEED, 0);
PRINT '  [OK] Cleared DETHI and DETHI_CAUHOI';

DELETE FROM KyThi;
DBCC CHECKIDENT ('KyThi', RESEED, 0);
PRINT '  [OK] Cleared KyThi';

DELETE FROM LUACHON;
DBCC CHECKIDENT ('LUACHON', RESEED, 0);

DELETE FROM CAUHOI;
DBCC CHECKIDENT ('CAUHOI', RESEED, 0);
PRINT '  [OK] Cleared LUACHON and CAUHOI';

DELETE FROM LOAICAUHOI;
DBCC CHECKIDENT ('LOAICAUHOI', RESEED, 0);
PRINT '  [OK] Cleared LOAICAUHOI';

DELETE FROM TAIKHOAN_VAITRO;
DBCC CHECKIDENT ('TAIKHOAN_VAITRO', RESEED, 0);

DELETE FROM TAIKHOAN;
DBCC CHECKIDENT ('TAIKHOAN', RESEED, 0);

DELETE FROM KHOA_PHONG;
DBCC CHECKIDENT ('KHOA_PHONG', RESEED, 0);

DELETE FROM VAITRO;
DBCC CHECKIDENT ('VAITRO', RESEED, 0);
PRINT '  [OK] Cleared TAIKHOAN, KHOA_PHONG, VAITRO';

-- Re-enable all foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";
PRINT '  [OK] Re-enabled all foreign key constraints';

PRINT '';
PRINT '========================================';
PRINT 'SEEDING DEFAULT DATA';
PRINT '========================================';

-- Insert default roles (1, 3, 5)
SET IDENTITY_INSERT VAITRO ON;
INSERT INTO VAITRO (Id, MaVaiTro, TenVaiTro, MoTa) VALUES 
(1, 'ADMIN', N'Quản trị viên', N'Quản trị viên hệ thống'),
(3, 'STUDENT', N'Thí sinh', N'Thí sinh tham gia thi'),
(5, 'DEPT_MANAGER', N'Quản lý khoa', N'Quản lý khoa tạo và quản lý đề thi');
SET IDENTITY_INSERT VAITRO OFF;
PRINT '  [OK] Seeded roles (ADMIN, STUDENT, DEPT_MANAGER)';

-- Insert default admin user (password: admin123)
-- Hash for admin123: $2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2
SET IDENTITY_INSERT TAIKHOAN ON;
INSERT INTO TAIKHOAN (Id, TenDangNhap, MaNhanVien, MatKhau, HoTen, IdVaiTro, TrangThai, NgayTao)
VALUES (1, 'admin', 'admin', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2', N'Quản trị viên hệ thống', 1, 1, GETDATE());
SET IDENTITY_INSERT TAIKHOAN OFF;
PRINT '  [OK] Seeded admin user with employee ID (admin/admin)';

-- Insert admin role mapping
INSERT INTO TAIKHOAN_VAITRO (IdTaiKhoan, IdVaiTro) VALUES (1, 1);
PRINT '  [OK] Seeded admin user role mapping';

PRINT '========================================';
PRINT 'DATABASE RESET & SEEDING COMPLETED';
PRINT '========================================';
GO
