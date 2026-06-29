-- Migration 012: Drop DANHMUCAUHOI table
-- Chạy script này để xóa hoàn toàn bảng DANHMUCAUHOI khỏi database

PRINT '=== Migration 012: Drop DANHMUCAUHOI Table ==='

IF EXISTS (SELECT * FROM sys.tables WHERE Name='DANHMUCAUHOI' AND Object_ID=Object_ID('DANHMUCAUHOI'))
BEGIN
    DROP TABLE DANHMUCAUHOI;
    PRINT '  + Dropped table DANHMUCAUHOI';
END
ELSE
BEGIN
    PRINT '  - Table DANHMUCAUHOI does not exist';
END
GO
