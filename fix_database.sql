-- ============================================================
-- FIX DATABASE: Thêm các cột còn thiếu cho BanTayVang
-- Chạy script này trong SQL Server Management Studio (SSMS)
-- hoặc: sqlcmd -S localhost -d HeThongBanTayVang -i fix_database.sql
-- ============================================================

USE HeThongBanTayVang;
GO

-- ============================================================
-- 1. Bảng DETHI: thiếu KhoaPhong, SoCauRandom, ChecksumData,
--               NguoiCapNhat, NgayCapNhat
-- ============================================================
PRINT 'Fixing table DETHI...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'KhoaPhong')
BEGIN
    ALTER TABLE DETHI ADD KhoaPhong NVARCHAR(200) NULL;
    PRINT '  + Added KhoaPhong to DETHI';
END
ELSE PRINT '  - KhoaPhong already exists in DETHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'SoCauRandom')
BEGIN
    ALTER TABLE DETHI ADD SoCauRandom INT NULL;
    PRINT '  + Added SoCauRandom to DETHI';
END
ELSE PRINT '  - SoCauRandom already exists in DETHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'ChecksumData')
BEGIN
    ALTER TABLE DETHI ADD ChecksumData NVARCHAR(500) NULL;
    PRINT '  + Added ChecksumData to DETHI';
END
ELSE PRINT '  - ChecksumData already exists in DETHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NguoiCapNhat')
BEGIN
    ALTER TABLE DETHI ADD NguoiCapNhat INT NULL;
    PRINT '  + Added NguoiCapNhat to DETHI';
END
ELSE PRINT '  - NguoiCapNhat already exists in DETHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NgayCapNhat')
BEGIN
    ALTER TABLE DETHI ADD NgayCapNhat DATETIME NULL;
    PRINT '  + Added NgayCapNhat to DETHI';
END
ELSE PRINT '  - NgayCapNhat already exists in DETHI';

-- ============================================================
-- 2. Bảng TAIKHOAN: thiếu Email, HoTen, IdVaiTro, TrangThai,
--                   NgayTao, NgayCapNhat, LanDangNhapCuoi
-- ============================================================
PRINT 'Fixing table TAIKHOAN...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'Email')
BEGIN
    ALTER TABLE TAIKHOAN ADD Email NVARCHAR(255) NULL;
    PRINT '  + Added Email to TAIKHOAN';
END
ELSE PRINT '  - Email already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'SoDienThoai')
BEGIN
    ALTER TABLE TAIKHOAN ADD SoDienThoai NVARCHAR(50) NULL;
    PRINT '  + Added SoDienThoai to TAIKHOAN';
END
ELSE PRINT '  - SoDienThoai already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'HoTen')
BEGIN
    ALTER TABLE TAIKHOAN ADD HoTen NVARCHAR(255) NULL;
    PRINT '  + Added HoTen to TAIKHOAN';
END
ELSE PRINT '  - HoTen already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'IdVaiTro')
BEGIN
    ALTER TABLE TAIKHOAN ADD IdVaiTro INT NULL;
    PRINT '  + Added IdVaiTro to TAIKHOAN';
END
ELSE PRINT '  - IdVaiTro already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'TrangThai')
BEGIN
    ALTER TABLE TAIKHOAN ADD TrangThai BIT NULL DEFAULT(1);
    PRINT '  + Added TrangThai to TAIKHOAN';
END
ELSE PRINT '  - TrangThai already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayTao')
BEGIN
    ALTER TABLE TAIKHOAN ADD NgayTao DATETIME NULL DEFAULT(GETDATE());
    PRINT '  + Added NgayTao to TAIKHOAN';
END
ELSE PRINT '  - NgayTao already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'NgayCapNhat')
BEGIN
    ALTER TABLE TAIKHOAN ADD NgayCapNhat DATETIME NULL;
    PRINT '  + Added NgayCapNhat to TAIKHOAN';
END
ELSE PRINT '  - NgayCapNhat already exists in TAIKHOAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'LanDangNhapCuoi')
BEGIN
    ALTER TABLE TAIKHOAN ADD LanDangNhapCuoi DATETIME NULL;
    PRINT '  + Added LanDangNhapCuoi to TAIKHOAN';
END
ELSE PRINT '  - LanDangNhapCuoi already exists in TAIKHOAN';

-- ============================================================
-- 3. Bảng BAITHI: thiếu ThoiGianBatDau, NgayCapNhat, LyDoKetThuc
-- ============================================================
PRINT 'Fixing table BAITHI...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'ThoiGianBatDau')
BEGIN
    ALTER TABLE BAITHI ADD ThoiGianBatDau DATETIME NULL;
    PRINT '  + Added ThoiGianBatDau to BAITHI';
END
ELSE PRINT '  - ThoiGianBatDau already exists in BAITHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'NgayCapNhat')
BEGIN
    ALTER TABLE BAITHI ADD NgayCapNhat DATETIME NULL;
    PRINT '  + Added NgayCapNhat to BAITHI';
END
ELSE PRINT '  - NgayCapNhat already exists in BAITHI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'BAITHI' AND COLUMN_NAME = 'LyDoKetThuc')
BEGIN
    ALTER TABLE BAITHI ADD LyDoKetThuc NVARCHAR(500) NULL;
    PRINT '  + Added LyDoKetThuc to BAITHI';
END
ELSE PRINT '  - LyDoKetThuc already exists in BAITHI';

-- ============================================================
-- 4. Bảng CANHBAOGIANLAN: thiếu MucDoNghiemTrong, CorrelationId
-- ============================================================
PRINT 'Fixing table CANHBAOGIANLAN...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CANHBAOGIANLAN' AND COLUMN_NAME = 'MucDoNghiemTrong')
BEGIN
    ALTER TABLE CANHBAOGIANLAN ADD MucDoNghiemTrong NVARCHAR(50) NULL;
    PRINT '  + Added MucDoNghiemTrong to CANHBAOGIANLAN';
END
ELSE PRINT '  - MucDoNghiemTrong already exists in CANHBAOGIANLAN';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CANHBAOGIANLAN' AND COLUMN_NAME = 'CorrelationId')
BEGIN
    ALTER TABLE CANHBAOGIANLAN ADD CorrelationId NVARCHAR(100) NULL;
    PRINT '  + Added CorrelationId to CANHBAOGIANLAN';
END
ELSE PRINT '  - CorrelationId already exists in CANHBAOGIANLAN';

-- ============================================================
-- 5. Bảng CAUHOI: thiếu KhoaPhong, HinhAnh (nếu chưa có)
-- ============================================================
PRINT 'Fixing table CAUHOI...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CAUHOI' AND COLUMN_NAME = 'KhoaPhong')
BEGIN
    ALTER TABLE CAUHOI ADD KhoaPhong NVARCHAR(100) NULL;
    PRINT '  + Added KhoaPhong to CAUHOI';
END
ELSE PRINT '  - KhoaPhong already exists in CAUHOI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CAUHOI' AND COLUMN_NAME = 'HinhAnh')
BEGIN
    ALTER TABLE CAUHOI ADD HinhAnh NVARCHAR(500) NULL;
    PRINT '  + Added HinhAnh to CAUHOI';
END
ELSE PRINT '  - HinhAnh already exists in CAUHOI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CAUHOI' AND COLUMN_NAME = 'DoKho')
BEGIN
    ALTER TABLE CAUHOI ADD DoKho NVARCHAR(50) NULL;
    PRINT '  + Added DoKho to CAUHOI';
END
ELSE PRINT '  - DoKho already exists in CAUHOI';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'CAUHOI' AND COLUMN_NAME = 'DaXoa')
BEGIN
    ALTER TABLE CAUHOI ADD DaXoa BIT NULL DEFAULT(0);
    PRINT '  + Added DaXoa to CAUHOI';
END
ELSE PRINT '  - DaXoa already exists in CAUHOI';

-- ============================================================
-- 6. Tạo bảng ExamAssignments nếu chưa tồn tại
-- ============================================================
PRINT 'Checking table ExamAssignments...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
               WHERE TABLE_NAME = 'ExamAssignments')
BEGIN
    CREATE TABLE ExamAssignments (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        ExamId      INT NOT NULL,
        UserId      INT NOT NULL,
        AssignedAt  DATETIME NOT NULL DEFAULT(GETUTCDATE()),
        AssignedBy  INT NULL,
        CustomStartTime DATETIME NULL,
        ExtraMinutes INT NULL,
        IsActive    BIT NOT NULL DEFAULT(1),
        Note        NVARCHAR(500) NULL,
        CONSTRAINT FK_ExamAssignments_Exam FOREIGN KEY (ExamId) REFERENCES DETHI(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ExamAssignments_User FOREIGN KEY (UserId) REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ExamAssignments_ExamId ON ExamAssignments(ExamId);
    CREATE INDEX IX_ExamAssignments_UserId ON ExamAssignments(UserId);
    PRINT '  + Created table ExamAssignments';
END
ELSE PRINT '  - ExamAssignments already exists';

-- ============================================================
-- 7. Tạo các bảng JWT nếu chưa tồn tại
-- ============================================================
PRINT 'Checking JWT tables...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'PhucHuy')
        EXEC('CREATE SCHEMA PhucHuy');

    CREATE TABLE PhucHuy.RefreshTokens (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Token       NVARCHAR(500) NOT NULL,
        UserId      INT NOT NULL,
        ExpiresAt   DATETIME NOT NULL,
        CreatedAt   DATETIME NOT NULL DEFAULT(GETUTCDATE()),
        RevokedAt   DATETIME NULL,
        IsRevoked   BIT NOT NULL DEFAULT(0),
        DeviceInfo  NVARCHAR(500) NULL,
        IpAddress   NVARCHAR(50) NULL
    );
    PRINT '  + Created table PhucHuy.RefreshTokens';
END
ELSE PRINT '  - RefreshTokens already exists';

-- Notifications table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Title       NVARCHAR(255) NOT NULL,
        Message     NVARCHAR(MAX) NOT NULL,
        Type        NVARCHAR(50) NULL DEFAULT('Info'),
        UserId      INT NULL,
        IsRead      BIT NOT NULL DEFAULT(0),
        CreatedAt   DATETIME NOT NULL DEFAULT(GETUTCDATE()),
        ReadAt      DATETIME NULL
    );
    PRINT '  + Created table Notifications';
END
ELSE PRINT '  - Notifications already exists';

-- AuditLogs table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NULL,
        Username    NVARCHAR(100) NULL,
        Action      NVARCHAR(100) NULL,
        Method      NVARCHAR(10) NULL,
        Path        NVARCHAR(500) NULL,
        IpAddress   NVARCHAR(50) NULL,
        Details     NVARCHAR(MAX) NULL,
        Timestamp   DATETIME NOT NULL DEFAULT(GETUTCDATE()),
        StatusCode  INT NULL
    );
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
    CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
    PRINT '  + Created table AuditLogs';
END
ELSE PRINT '  - AuditLogs already exists';

-- ============================================================
-- XONG - Kiểm tra kết quả
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'DONE! Verify result:';
PRINT '============================================================';

SELECT 
    t.TABLE_NAME,
    COUNT(c.COLUMN_NAME) AS TotalColumns
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_NAME IN ('DETHI','TAIKHOAN','BAITHI','CAUHOI','CANHBAOGIANLAN',
                        'ExamAssignments','Notifications','AuditLogs')
GROUP BY t.TABLE_NAME
ORDER BY t.TABLE_NAME;
GO
