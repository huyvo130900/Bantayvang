-- Migration 008: Exam Visibility Toggle (CongBoKetQua)
-- Date: 2026-05

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='CongBoKetQua' AND Object_ID=Object_ID('DETHI'))
BEGIN
    ALTER TABLE DETHI ADD 
        CongBoKetQua   BIT      NOT NULL DEFAULT 0,
        NguoiCongBo    INT      NULL,
        ThoiGianCongBo DATETIME NULL;
    PRINT 'Added CongBoKetQua, NguoiCongBo, ThoiGianCongBo to DETHI';
END

PRINT 'Migration 008 completed';
