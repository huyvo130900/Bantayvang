-- === Migration 010: Link KyThi to KhoaPhong ===

PRINT '=== Migration 010: Link KyThi to KhoaPhong ==='

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='KhoaPhongId' AND Object_ID=Object_ID('KyThi'))
BEGIN
    ALTER TABLE KyThi ADD KhoaPhongId INT NULL;
    PRINT 'Added KhoaPhongId to KyThi';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_KyThi_KhoaPhong')
BEGIN
    ALTER TABLE KyThi ADD CONSTRAINT FK_KyThi_KhoaPhong
        FOREIGN KEY (KhoaPhongId) REFERENCES KHOA_PHONG(Id) ON DELETE SET NULL;
    PRINT 'Added FK_KyThi_KhoaPhong constraint';
END

IF EXISTS (SELECT * FROM sys.columns WHERE Name='LoaiKyThi' AND Object_ID=Object_ID('KyThi'))
BEGIN
    ALTER TABLE KyThi DROP COLUMN LoaiKyThi;
    PRINT 'Dropped LoaiKyThi from KyThi';
END
