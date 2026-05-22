-- ============================================
-- ALL MIGRATIONS - Chạy 1 lần để cập nhật toàn bộ DB
-- ============================================
-- Author: BanTayVang Team
-- Description: Apply all schema changes for the system
-- ============================================

USE HeThongBanTayVang;
GO

PRINT '====================================';
PRINT 'STARTING ALL MIGRATIONS';
PRINT '====================================';
PRINT '';

-- ============================================
-- 1. JWT Authentication Tables (RefreshTokens, UserSessions)
-- ============================================
PRINT '[1/5] JWT Authentication Tables...';

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RefreshTokens' AND xtype='U')
BEGIN
    CREATE TABLE RefreshTokens (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Token nvarchar(500) NOT NULL UNIQUE,
        UserId int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        ExpiresAt datetime2 NOT NULL,
        IsUsed bit NOT NULL DEFAULT 0,
        IsRevoked bit NOT NULL DEFAULT 0,
        IpAddress nvarchar(45) NULL,
        UserAgent nvarchar(500) NULL,
        CONSTRAINT FK_RefreshTokens_UserId FOREIGN KEY (UserId) REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
    CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
    PRINT '  [OK] RefreshTokens table created';
END
ELSE PRINT '  [SKIP] RefreshTokens already exists';

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserSessions' AND xtype='U')
BEGIN
    CREATE TABLE UserSessions (
        Id int IDENTITY(1,1) PRIMARY KEY,
        SessionId nvarchar(100) NOT NULL UNIQUE,
        UserId int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        ExpiresAt datetime2 NOT NULL,
        LastActivityAt datetime2 NULL,
        IpAddress nvarchar(45) NULL,
        UserAgent nvarchar(500) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        EndReason nvarchar(50) NULL,
        CONSTRAINT FK_UserSessions_UserId FOREIGN KEY (UserId) REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_UserSessions_SessionId ON UserSessions(SessionId);
    CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
    PRINT '  [OK] UserSessions table created';
END
ELSE PRINT '  [SKIP] UserSessions already exists';
GO

-- ============================================
-- 2. TAIKHOAN: Add JWT auth columns
-- ============================================
PRINT '';
PRINT '[2/5] TAIKHOAN columns...';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'Email')
    ALTER TABLE TAIKHOAN ADD Email nvarchar(255) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'HoTen')
    ALTER TABLE TAIKHOAN ADD HoTen nvarchar(255) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'IdVaiTro')
    ALTER TABLE TAIKHOAN ADD IdVaiTro int NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'TrangThai')
    ALTER TABLE TAIKHOAN ADD TrangThai bit NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayTao')
    ALTER TABLE TAIKHOAN ADD NgayTao datetime2 NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayCapNhat')
    ALTER TABLE TAIKHOAN ADD NgayCapNhat datetime2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'LanDangNhapCuoi')
    ALTER TABLE TAIKHOAN ADD LanDangNhapCuoi datetime2 NULL;

PRINT '  [OK] TAIKHOAN columns updated';
GO

-- ============================================
-- 3. DETHI / BAITHI / CANHBAOGIANLAN columns
-- ============================================
PRINT '';
PRINT '[3/5] DETHI / BAITHI / CANHBAOGIANLAN columns...';

-- DETHI
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'ChecksumData')
    ALTER TABLE DETHI ADD ChecksumData nvarchar(500) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NguoiCapNhat')
    ALTER TABLE DETHI ADD NguoiCapNhat int NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NgayCapNhat')
    ALTER TABLE DETHI ADD NgayCapNhat datetime2 NULL;

-- BAITHI
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'ThoiGianBatDau')
    ALTER TABLE BAITHI ADD ThoiGianBatDau datetime2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'NgayCapNhat')
    ALTER TABLE BAITHI ADD NgayCapNhat datetime2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'LyDoKetThuc')
    ALTER TABLE BAITHI ADD LyDoKetThuc nvarchar(255) NULL;

-- CANHBAOGIANLAN
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CANHBAOGIANLAN' AND COLUMN_NAME = 'MucDoNghiemTrong')
    ALTER TABLE CANHBAOGIANLAN ADD MucDoNghiemTrong nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CANHBAOGIANLAN' AND COLUMN_NAME = 'CorrelationId')
    ALTER TABLE CANHBAOGIANLAN ADD CorrelationId nvarchar(100) NULL;

PRINT '  [OK] Columns updated';
GO

-- ============================================
-- 4. Notifications Table
-- ============================================
PRINT '';
PRINT '[4/5] Notifications table...';

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    CREATE TABLE Notifications (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NULL,
        Title nvarchar(255) NOT NULL,
        Message nvarchar(1000) NOT NULL,
        Type nvarchar(50) NOT NULL DEFAULT 'Info',
        IsRead bit NOT NULL DEFAULT 0,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ReadAt datetime2 NULL,
        RelatedUrl nvarchar(500) NULL,
        CONSTRAINT FK_Notifications_UserId FOREIGN KEY (UserId) REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
    CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);
    PRINT '  [OK] Notifications table created';
END
ELSE PRINT '  [SKIP] Notifications already exists';
GO

-- ============================================
-- 5. Default data: Roles & Admin user
-- ============================================
PRINT '';
PRINT '[5/5] Default data...';

-- Default roles
IF EXISTS (SELECT * FROM sysobjects WHERE name='VAITRO' AND xtype='U')
BEGIN
    IF NOT EXISTS (SELECT * FROM VAITRO WHERE Id = 1)
    BEGIN
        SET IDENTITY_INSERT VAITRO ON;
        INSERT INTO VAITRO (Id, MaVaiTro, TenVaiTro, MoTa) VALUES 
        (1, 'ADMIN', N'Quản trị viên', N'Quản trị viên hệ thống'),
        (2, 'TEACHER', N'Giảng viên', N'Giảng viên tạo và quản lý đề thi'),
        (3, 'STUDENT', N'Học viên', N'Học viên tham gia thi'),
        (4, 'SUPERVISOR', N'Giám sát', N'Giám sát viên theo dõi kỳ thi');
        SET IDENTITY_INSERT VAITRO OFF;
        PRINT '  [OK] Default roles inserted';
    END
END

-- Default admin user (password: admin123)
IF NOT EXISTS (SELECT * FROM TAIKHOAN WHERE TenDangNhap = 'admin')
BEGIN
    INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, HoTen, IdVaiTro, TrangThai, NgayTao)
    VALUES ('admin',
            '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2',
            'admin@bantayvang.vn',
            N'Quản trị viên hệ thống',
            1, 1, GETDATE());
    PRINT '  [OK] Default admin user created (admin/admin123)';
END
ELSE
BEGIN
    UPDATE TAIKHOAN 
    SET MatKhau = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2',
        TrangThai = 1,
        IdVaiTro = ISNULL(IdVaiTro, 1),
        Email = ISNULL(Email, 'admin@bantayvang.vn'),
        HoTen = ISNULL(HoTen, N'Quản trị viên hệ thống')
    WHERE TenDangNhap = 'admin';
    PRINT '  [OK] Admin user updated';
END
GO

-- ============================================
-- COMPLETED
-- ============================================
PRINT '';
PRINT '====================================';
PRINT 'ALL MIGRATIONS COMPLETED!';
PRINT 'Default credentials: admin / admin123';
PRINT 'IMPORTANT: Use API /api/Seed/reset-admin-password to set correct password';
PRINT '====================================';
GO