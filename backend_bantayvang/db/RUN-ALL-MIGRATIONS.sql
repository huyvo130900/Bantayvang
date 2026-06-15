-- =============================================================
-- BAN TAY VANG - Chạy toàn bộ migrations theo thứ tự
-- =============================================================

PRINT '=== Migration 006: Department Manager ==='

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
    PRINT 'Created KHOA_PHONG';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='IdKhoaQuanLy' AND Object_ID=Object_ID('TAIKHOAN'))
BEGIN
    ALTER TABLE TAIKHOAN ADD IdKhoaQuanLy INT NULL;
    PRINT 'Added IdKhoaQuanLy to TAIKHOAN';
END

-- Role 5
IF NOT EXISTS (SELECT * FROM VAITRO WHERE Id = 5)
BEGIN
    SET IDENTITY_INSERT VAITRO ON;
    INSERT INTO VAITRO (Id, MaVaiTro, TenVaiTro, MoTa)
    VALUES (5, 'DEPT_MANAGER', N'Quản lý Khoa', N'Quản lý câu hỏi và đề thi theo khoa');
    SET IDENTITY_INSERT VAITRO OFF;
    PRINT 'Inserted DEPT_MANAGER role';
END

-- FK constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_KHOA_PHONG_DeptManager')
    ALTER TABLE KHOA_PHONG ADD CONSTRAINT FK_KHOA_PHONG_DeptManager
        FOREIGN KEY (DeptManagerId) REFERENCES TAIKHOAN(Id);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_TAIKHOAN_KhoaQuanLy')
    ALTER TABLE TAIKHOAN ADD CONSTRAINT FK_TAIKHOAN_KhoaQuanLy
        FOREIGN KEY (IdKhoaQuanLy) REFERENCES KHOA_PHONG(Id);

-- Seed departments
IF NOT EXISTS (SELECT * FROM KHOA_PHONG WHERE MaKhoa='KHOA_NOI')
BEGIN
    INSERT INTO KHOA_PHONG (MaKhoa, TenKhoa, TrangThai) VALUES
        ('KHOA_NOI',      N'Khoa Nội',                    1),
        ('KHOA_NGOAI',    N'Khoa Ngoại',                  1),
        ('KHOA_SAN',      N'Khoa Sản',                    1),
        ('KHOA_NHI',      N'Khoa Nhi',                    1),
        ('KHOA_CAPCUU',   N'Khoa Cấp cứu',                1),
        ('KHOA_CDHA',     N'Khoa Chẩn đoán hình ảnh',     1),
        ('KHOA_PTTT',     N'Khoa Phẫu thuật - Thủ thuật', 1),
        ('PHONG_DIDUONG', N'Phòng Điều dưỡng',            1);
    PRINT 'Seeded 8 departments';
END

PRINT '=== Migration 007: Enhanced Audit Log ==='

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='IdTaiKhoan' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD IdTaiKhoan INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='TenDangNhap' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD TenDangNhap NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='PhuongThuc' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD PhuongThuc NVARCHAR(10) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='DuongDan' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD DuongDan NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='MaHttp' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD MaHttp INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='KhoaPhong' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD KhoaPhong NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='UserAgent' AND Object_ID=Object_ID('LOGTHAOTAC'))
    ALTER TABLE LOGTHAOTAC ADD UserAgent NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_LOGTHAOTAC_TaiKhoan')
    ALTER TABLE LOGTHAOTAC ADD CONSTRAINT FK_LOGTHAOTAC_TaiKhoan
        FOREIGN KEY (IdTaiKhoan) REFERENCES TAIKHOAN(Id) ON DELETE SET NULL;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_LOGTHAOTAC_IdTaiKhoan')
    CREATE INDEX IX_LOGTHAOTAC_IdTaiKhoan ON LOGTHAOTAC(IdTaiKhoan);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_LOGTHAOTAC_ThoiGian')
    CREATE INDEX IX_LOGTHAOTAC_ThoiGian ON LOGTHAOTAC(ThoiGian DESC);

PRINT '=== Migration 008: Exam Visibility ==='

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='CongBoKetQua' AND Object_ID=Object_ID('DETHI'))
    ALTER TABLE DETHI ADD CongBoKetQua BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='NguoiCongBo' AND Object_ID=Object_ID('DETHI'))
    ALTER TABLE DETHI ADD NguoiCongBo INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='ThoiGianCongBo' AND Object_ID=Object_ID('DETHI'))
    ALTER TABLE DETHI ADD ThoiGianCongBo DATETIME NULL;

PRINT '=== Migration 009: LICHSU_THI ==='

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LICHSU_THI' AND xtype='U')
BEGIN
    CREATE TABLE LICHSU_THI (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        IdThiSinh        INT          NOT NULL,
        IdDeThi          INT          NOT NULL,
        LanThi           INT          NOT NULL DEFAULT 1,
        ThoiGianBatDau   DATETIME     NULL,
        ThoiGianNop      DATETIME     NULL,
        SoCauDung        INT          NULL,
        TongSoCau        INT          NULL,
        SoLanGianLan     INT          NOT NULL DEFAULT 0,
        TrangThai        NVARCHAR(50) NOT NULL DEFAULT 'DangThi',
        NgayTao          DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_LICHSU_THI_ThiSinh FOREIGN KEY (IdThiSinh) REFERENCES TAIKHOAN(Id),
        CONSTRAINT FK_LICHSU_THI_DeThi   FOREIGN KEY (IdDeThi)   REFERENCES DETHI(Id)
    );
    CREATE INDEX IX_LICHSU_THI_ThiSinh ON LICHSU_THI(IdThiSinh);
    CREATE INDEX IX_LICHSU_THI_DeThi   ON LICHSU_THI(IdDeThi);
    PRINT 'Created LICHSU_THI';
END

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

PRINT '=== Migration 011: Drop IdDanhMuc from CAUHOI ==='

DECLARE @ConstraintName NVARCHAR(255);
SELECT @ConstraintName = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
WHERE fk.parent_object_id = OBJECT_ID('CAUHOI')
  AND c.name = 'IdDanhMuc';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE CAUHOI DROP CONSTRAINT ' + @ConstraintName);
    PRINT '  + Dropped foreign key constraint: ' + @ConstraintName;
END

IF EXISTS (SELECT * FROM sys.columns WHERE Name='IdDanhMuc' AND Object_ID=Object_ID('CAUHOI'))
BEGIN
    ALTER TABLE CAUHOI DROP COLUMN IdDanhMuc;
    PRINT '  + Dropped column IdDanhMuc from CAUHOI';
END

PRINT '=== Migration 012: Drop DANHMUCAUHOI Table ==='

IF EXISTS (SELECT * FROM sys.tables WHERE Name='DANHMUCAUHOI' AND Object_ID=Object_ID('DANHMUCAUHOI'))
BEGIN
    DROP TABLE DANHMUCAUHOI;
    PRINT '  + Dropped table DANHMUCAUHOI';
END

PRINT '=== Migration 015: Set Default DoKho to CAUHOI ==='

UPDATE CAUHOI SET DoKho = '1' WHERE DoKho IS NULL OR LTRIM(RTRIM(DoKho)) = '';

IF NOT EXISTS (
    SELECT * FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID('CAUHOI') 
      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('CAUHOI'), 'DoKho', 'ColumnId')
)
BEGIN
    ALTER TABLE CAUHOI ADD CONSTRAINT DF_CAUHOI_DoKho DEFAULT '1' FOR DoKho;
    PRINT 'Added DEFAULT constraint for DoKho in CAUHOI';
END

PRINT '=== ALL MIGRATIONS COMPLETED ==='
