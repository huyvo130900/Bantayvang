-- Migration: Thêm cột KhoaPhong và SoCauRandom vào bảng DETHI
-- Chạy script này trên database để cập nhật schema

-- Thêm cột KhoaPhong (khoa/phòng nguồn câu hỏi)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'KhoaPhong')
BEGIN
    ALTER TABLE DETHI ADD KhoaPhong NVARCHAR(200) NULL;
    PRINT 'Added KhoaPhong column to DETHI';
END

-- Thêm cột SoCauRandom (số câu hỏi random từ ngân hàng)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'SoCauRandom')
BEGIN
    ALTER TABLE DETHI ADD SoCauRandom INT NULL;
    PRINT 'Added SoCauRandom column to DETHI';
END

-- Thêm ChecksumData nếu chưa có (lưu config: KHOA:xxx|SO_CAU:xxx|POOL:xxx)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'ChecksumData')
BEGIN
    ALTER TABLE DETHI ADD ChecksumData NVARCHAR(500) NULL;
    PRINT 'Added ChecksumData column to DETHI';
END

-- Thêm NguoiCapNhat và NgayCapNhat nếu chưa có
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NguoiCapNhat')
BEGIN
    ALTER TABLE DETHI ADD NguoiCapNhat INT NULL;
    PRINT 'Added NguoiCapNhat column to DETHI';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'NgayCapNhat')
BEGIN
    ALTER TABLE DETHI ADD NgayCapNhat DATETIME NULL;
    PRINT 'Added NgayCapNhat column to DETHI';
END

PRINT 'Migration completed successfully';


