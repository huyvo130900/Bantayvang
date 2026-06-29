-- Migration 009: Bảng LICHSU_THI & tự động tính điểm
-- Date: 2026-05

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
        -- DiemSo tính tự động: (SoCauDung * 10.0 / TongSoCau)
        DiemSo           AS (CASE WHEN TongSoCau > 0 THEN CAST(SoCauDung * 10.0 / TongSoCau AS DECIMAL(5,2)) ELSE 0 END) PERSISTED,
        SoLanGianLan     INT          NOT NULL DEFAULT 0,
        -- TrangThai: HoanThanh | BiBHuyGianLan | DangThi
        TrangThai        NVARCHAR(50) NOT NULL DEFAULT 'DangThi',
        -- XepLoai tự động theo DiemSo
        XepLoai          AS (
            CASE
                WHEN SoCauDung * 10.0 / NULLIF(TongSoCau,0) >= 9.0 THEN N'Xuất sắc'
                WHEN SoCauDung * 10.0 / NULLIF(TongSoCau,0) >= 8.0 THEN N'Giỏi'
                WHEN SoCauDung * 10.0 / NULLIF(TongSoCau,0) >= 6.5 THEN N'Khá'
                WHEN SoCauDung * 10.0 / NULLIF(TongSoCau,0) >= 5.0 THEN N'Trung bình'
                ELSE N'Không đạt'
            END
        ) PERSISTED,
        NgayTao          DATETIME     NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_LICHSU_THI_ThiSinh FOREIGN KEY (IdThiSinh) REFERENCES TAIKHOAN(Id),
        CONSTRAINT FK_LICHSU_THI_DeThi   FOREIGN KEY (IdDeThi)   REFERENCES DETHI(Id)
    );

    CREATE INDEX IX_LICHSU_THI_ThiSinh ON LICHSU_THI (IdThiSinh);
    CREATE INDEX IX_LICHSU_THI_DeThi   ON LICHSU_THI (IdDeThi);

    PRINT 'Created LICHSU_THI table';
END

PRINT 'Migration 009 completed';
