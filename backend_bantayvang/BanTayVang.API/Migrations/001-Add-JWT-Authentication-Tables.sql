-- JWT Authentication Tables Migration
-- OWASP A07: Identification and Authentication Failures prevention
-- Created: $(date)

USE HeThongBanTayVang;
GO

-- Create RefreshTokens table
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
        
        CONSTRAINT FK_RefreshTokens_UserId FOREIGN KEY (UserId) 
            REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
    CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
    CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
    
    PRINT 'RefreshTokens table created successfully';
END
ELSE
BEGIN
    PRINT 'RefreshTokens table already exists';
END
GO

-- Create UserSessions table
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
        
        CONSTRAINT FK_UserSessions_UserId FOREIGN KEY (UserId) 
            REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE INDEX IX_UserSessions_SessionId ON UserSessions(SessionId);
    CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
    CREATE INDEX IX_UserSessions_IsActive ON UserSessions(IsActive);
    CREATE INDEX IX_UserSessions_ExpiresAt ON UserSessions(ExpiresAt);
    
    PRINT 'UserSessions table created successfully';
END
ELSE
BEGIN
    PRINT 'UserSessions table already exists';
END
GO

-- Add missing columns to TAIKHOAN table if they don't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'Email')
BEGIN
    ALTER TABLE TAIKHOAN ADD Email nvarchar(255) NULL;
    PRINT 'Email column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'HoTen')
BEGIN
    ALTER TABLE TAIKHOAN ADD HoTen nvarchar(255) NULL;
    PRINT 'HoTen column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'IdVaiTro')
BEGIN
    ALTER TABLE TAIKHOAN ADD IdVaiTro int NULL;
    PRINT 'IdVaiTro column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'TrangThai')
BEGIN
    ALTER TABLE TAIKHOAN ADD TrangThai bit NULL DEFAULT 1;
    PRINT 'TrangThai column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayTao')
BEGIN
    ALTER TABLE TAIKHOAN ADD NgayTao datetime2 NULL DEFAULT GETDATE();
    PRINT 'NgayTao column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayCapNhat')
BEGIN
    ALTER TABLE TAIKHOAN ADD NgayCapNhat datetime2 NULL;
    PRINT 'NgayCapNhat column added to TAIKHOAN table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'LanDangNhapCuoi')
BEGIN
    ALTER TABLE TAIKHOAN ADD LanDangNhapCuoi datetime2 NULL;
    PRINT 'LanDangNhapCuoi column added to TAIKHOAN table';
END
GO

-- Create default admin user if not exists
IF NOT EXISTS (SELECT * FROM TAIKHOAN WHERE TenDangNhap = 'admin')
BEGIN
    INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, HoTen, IdVaiTro, TrangThai, NgayTao)
    VALUES (
        'admin',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2', -- password: admin123
        'admin@bantayvang.vn',
        'Quản trị viên hệ thống',
        1, -- Admin role
        1, -- Active
        GETDATE()
    );
    PRINT 'Default admin user created (username: admin, password: admin123)';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END
GO

-- Create default roles if VAITRO table exists and is empty
IF EXISTS (SELECT * FROM sysobjects WHERE name='VAITRO' AND xtype='U')
BEGIN
    IF NOT EXISTS (SELECT * FROM VAITRO WHERE Id = 1)
    BEGIN
        INSERT INTO VAITRO (Id, MaVaiTro, TenVaiTro, MoTa) VALUES 
        (1, 'ADMIN', 'Quản trị viên', 'Quản trị viên hệ thống'),
        (2, 'TEACHER', 'Giảng viên', 'Giảng viên tạo và quản lý đề thi'),
        (3, 'STUDENT', 'Học viên', 'Học viên tham gia thi'),
        (4, 'SUPERVISOR', 'Giám sát', 'Giám sát viên theo dõi kỳ thi');
        
        PRINT 'Default roles created';
    END
    ELSE
    BEGIN
        PRINT 'Roles already exist';
    END
END
GO

-- Create stored procedure for cleanup expired tokens (optional)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'CleanupExpiredTokens')
BEGIN
    EXEC('
    CREATE PROCEDURE CleanupExpiredTokens
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @DeletedTokens int = 0;
        DECLARE @DeletedSessions int = 0;
        
        -- Delete expired refresh tokens
        DELETE FROM RefreshTokens 
        WHERE ExpiresAt <= GETDATE() OR IsUsed = 1 OR IsRevoked = 1;
        SET @DeletedTokens = @@ROWCOUNT;
        
        -- Mark expired sessions as inactive
        UPDATE UserSessions 
        SET IsActive = 0, EndReason = ''Expired'', LastActivityAt = GETDATE()
        WHERE ExpiresAt <= GETDATE() AND IsActive = 1;
        SET @DeletedSessions = @@ROWCOUNT;
        
        PRINT ''Cleanup completed: '' + CAST(@DeletedTokens AS varchar) + '' tokens deleted, '' + CAST(@DeletedSessions AS varchar) + '' sessions expired'';
    END
    ');
    PRINT 'CleanupExpiredTokens stored procedure created';
END
GO

PRINT 'JWT Authentication migration completed successfully!';
PRINT 'Default admin credentials: username=admin, password=admin123';
PRINT 'Please change the default admin password after first login.';
GO