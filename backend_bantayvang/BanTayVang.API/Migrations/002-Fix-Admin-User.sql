-- Fix Admin User - Update existing admin account with required fields
-- Run this if admin user already existed before migration

USE HeThongBanTayVang;
GO

-- Update admin user with required fields
UPDATE TAIKHOAN 
SET 
    MatKhau = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VjPoyNdO2', -- password: admin123
    Email = ISNULL(Email, 'admin@bantayvang.vn'),
    HoTen = ISNULL(HoTen, 'Quản trị viên hệ thống'),
    IdVaiTro = ISNULL(IdVaiTro, 1), -- Admin role
    TrangThai = 1, -- Active (force set to true)
    NgayTao = ISNULL(NgayTao, GETDATE())
WHERE TenDangNhap = 'admin';

PRINT 'Admin user updated successfully';

-- Verify admin user
SELECT 
    Id, 
    TenDangNhap, 
    Email, 
    HoTen, 
    IdVaiTro, 
    TrangThai,
    NgayTao,
    LanDangNhapCuoi
FROM TAIKHOAN 
WHERE TenDangNhap = 'admin';

PRINT 'Default admin credentials: username=admin, password=admin123';
GO