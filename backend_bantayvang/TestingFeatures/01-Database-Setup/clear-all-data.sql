-- Clear All Data Script for HeThongBanTayVang Database
-- Run this to delete all data from all tables while preserving table structure
-- CAUTION: This will delete ALL data - use only for testing!

USE [HeThongBanTayVang]
GO

-- Disable foreign key constraints temporarily
EXEC sp_msforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"
GO

-- Delete data in reverse dependency order to avoid FK violations

-- Level 1: Tables with no dependencies on them (leaf tables)
DELETE FROM [CHITIETLAMBAI];
DELETE FROM [CANHBAOGIANLAN];
DELETE FROM [LOGTHAOTAC];
DELETE FROM [DETHI_CAUHOI];
DELETE FROM [LUACHON];
DELETE FROM [PHIENDANGNHAP];
DELETE FROM [TAIKHOAN_VAITRO];

-- Level 2: Tables that depend on Level 1
DELETE FROM [BAITHI];

-- Level 3: Tables that depend on Level 2
DELETE FROM [CAUHOI];
DELETE FROM [DETHI];

-- Level 4: Master data tables
DELETE FROM [DANHMUCAUHOI];
DELETE FROM [LOAICAUHOI];
DELETE FROM [TAIKHOAN];
DELETE FROM [VAITRO];

-- Re-enable foreign key constraints
EXEC sp_msforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"
GO

-- Reset identity columns to start from 1
DBCC CHECKIDENT ('BAITHI', RESEED, 0);
DBCC CHECKIDENT ('CANHBAOGIANLAN', RESEED, 0);
DBCC CHECKIDENT ('CAUHOI', RESEED, 0);
DBCC CHECKIDENT ('CHITIETLAMBAI', RESEED, 0);
DBCC CHECKIDENT ('DANHMUCAUHOI', RESEED, 0);
DBCC CHECKIDENT ('DETHI', RESEED, 0);
DBCC CHECKIDENT ('DETHI_CAUHOI', RESEED, 0);
DBCC CHECKIDENT ('LOAICAUHOI', RESEED, 0);
DBCC CHECKIDENT ('LOGTHAOTAC', RESEED, 0);
DBCC CHECKIDENT ('LUACHON', RESEED, 0);
DBCC CHECKIDENT ('PHIENDANGNHAP', RESEED, 0);
DBCC CHECKIDENT ('TAIKHOAN', RESEED, 0);
DBCC CHECKIDENT ('TAIKHOAN_VAITRO', RESEED, 0);
DBCC CHECKIDENT ('VAITRO', RESEED, 0);

PRINT 'All data cleared successfully. Identity columns reset to start from 1.';