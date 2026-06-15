-- Migration 007: Thêm trường công bố điểm riêng cho từng thí sinh
-- Chạy sau migration 006

-- Thêm cột CongBoRieng vào bảng Baithi
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Baithi' AND COLUMN_NAME = 'CongBoRieng')
BEGIN
    ALTER TABLE Baithi ADD CongBoRieng BIT NOT NULL DEFAULT(0);
    PRINT 'Added CongBoRieng to Baithi';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Baithi' AND COLUMN_NAME = 'ThoiGianCongBoRieng')
BEGIN
    ALTER TABLE Baithi ADD ThoiGianCongBoRieng DATETIME NULL;
    PRINT 'Added ThoiGianCongBoRieng to Baithi';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Baithi' AND COLUMN_NAME = 'NguoiCongBoRieng')
BEGIN
    ALTER TABLE Baithi ADD NguoiCongBoRieng INT NULL;
    PRINT 'Added NguoiCongBoRieng to Baithi';
END

-- Index để tối ưu truy vấn
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Baithi_CongBoRieng')
BEGIN
    CREATE INDEX IX_Baithi_CongBoRieng ON Baithi(IdDeThi, CongBoRieng);
    PRINT 'Created index IX_Baithi_CongBoRieng';
END

PRINT 'Migration 007 completed';
