-- Migration 007: Enhanced Audit Log
-- Add proper UserId, Username, Method, Path, StatusCode, KhoaPhong columns

-- 1. Add IdTaiKhoan (proper FK)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='IdTaiKhoan' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD IdTaiKhoan INT NULL;
    ALTER TABLE LOGTHAOTAC ADD CONSTRAINT FK_LOGTHAOTAC_TaiKhoan 
        FOREIGN KEY (IdTaiKhoan) REFERENCES TAIKHOAN(Id);
    PRINT 'Added IdTaiKhoan to LOGTHAOTAC';
END

-- 2. Add TenDangNhap (cached username)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='TenDangNhap' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD TenDangNhap NVARCHAR(100) NULL;
    PRINT 'Added TenDangNhap to LOGTHAOTAC';
END

-- 3. Add PhuongThuc (HTTP Method)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='PhuongThuc' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD PhuongThuc NVARCHAR(10) NULL;
    PRINT 'Added PhuongThuc to LOGTHAOTAC';
END

-- 4. Add DuongDan (API Path)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='DuongDan' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD DuongDan NVARCHAR(500) NULL;
    PRINT 'Added DuongDan to LOGTHAOTAC';
END

-- 5. Add MaHttp (HTTP Status Code)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='MaHttp' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD MaHttp INT NULL;
    PRINT 'Added MaHttp to LOGTHAOTAC';
END

-- 6. Add KhoaPhong
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='KhoaPhong' AND Object_ID=Object_ID('LOGTHAOTAC'))
BEGIN
    ALTER TABLE LOGTHAOTAC ADD KhoaPhong NVARCHAR(100) NULL;
    PRINT 'Added KhoaPhong to LOGTHAOTAC';
END

-- 7. Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_LOGTHAOTAC_IdTaiKhoan')
    CREATE INDEX IX_LOGTHAOTAC_IdTaiKhoan ON LOGTHAOTAC(IdTaiKhoan);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_LOGTHAOTAC_ThoiGian')
    CREATE INDEX IX_LOGTHAOTAC_ThoiGian ON LOGTHAOTAC(ThoiGian DESC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_LOGTHAOTAC_KhoaPhong')
    CREATE INDEX IX_LOGTHAOTAC_KhoaPhong ON LOGTHAOTAC(KhoaPhong);

PRINT 'Migration 007 completed';
