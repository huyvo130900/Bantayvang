-- Migration 006: Department Manager & KhoaPhong table
-- Date: 2026-05

-- 1. Tạo bảng KHOA_PHONG
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KHOA_PHONG' AND xtype='U')
BEGIN
    CREATE TABLE KHOA_PHONG (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        MaKhoa      NVARCHAR(50)   NOT NULL UNIQUE,
        TenKhoa     NVARCHAR(255)  NOT NULL,
        MoTa        NVARCHAR(1000) NULL,
        TrangThai   BIT            NOT NULL DEFAULT 1,
        DeptManagerId INT          NULL,
        NguoiTao    INT            NULL,
        NgayTao     DATETIME       NOT NULL DEFAULT GETDATE(),
        NgayCapNhat DATETIME       NULL
    );
    PRINT 'Created KHOA_PHONG table';
END

-- 2. Thêm IdKhoaQuanLy vào TAIKHOAN
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'IdKhoaQuanLy' AND Object_ID = Object_ID('TAIKHOAN'))
BEGIN
    ALTER TABLE TAIKHOAN ADD IdKhoaQuanLy INT NULL;
    PRINT 'Added IdKhoaQuanLy to TAIKHOAN';
END

-- 3. Thêm role DEPT_MANAGER (ID=5)
IF NOT EXISTS (SELECT * FROM VAITRO WHERE Id = 5)
BEGIN
    SET IDENTITY_INSERT VAITRO ON;
    INSERT INTO VAITRO (Id, MaVaiTro, TenVaiTro, MoTa)
    VALUES (5, 'DEPT_MANAGER', N'Quản lý Khoa', N'Quản lý câu hỏi và đề thi theo khoa');
    SET IDENTITY_INSERT VAITRO OFF;
    PRINT 'Inserted DEPT_MANAGER role (ID=5)';
END

-- 4. FK constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KHOA_PHONG_DeptManager')
BEGIN
    ALTER TABLE KHOA_PHONG ADD CONSTRAINT FK_KHOA_PHONG_DeptManager
        FOREIGN KEY (DeptManagerId) REFERENCES TAIKHOAN(Id);
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TAIKHOAN_KhoaQuanLy')
BEGIN
    ALTER TABLE TAIKHOAN ADD CONSTRAINT FK_TAIKHOAN_KhoaQuanLy
        FOREIGN KEY (IdKhoaQuanLy) REFERENCES KHOA_PHONG(Id);
END

-- 5. Seed một số khoa mẫu cho bệnh viện
IF NOT EXISTS (SELECT * FROM KHOA_PHONG WHERE MaKhoa = 'KHOA_NOI')
BEGIN
    INSERT INTO KHOA_PHONG (MaKhoa, TenKhoa, TrangThai)
    VALUES
        ('KHOA_NOI',     N'Khoa Nội',                    1),
        ('KHOA_NGOAI',   N'Khoa Ngoại',                  1),
        ('KHOA_SAN',     N'Khoa Sản',                    1),
        ('KHOA_NHI',     N'Khoa Nhi',                    1),
        ('KHOA_CAPCUU',  N'Khoa Cấp cứu',                1),
        ('KHOA_CDHA',    N'Khoa Chẩn đoán hình ảnh',     1),
        ('KHOA_PTTH',    N'Khoa Phẫu thuật - Thủ thuật', 1),
        ('PHONG_DIDUONG',N'Phòng Điều dưỡng',            1);
    PRINT 'Inserted sample departments';
END

PRINT 'Migration 006 completed';
