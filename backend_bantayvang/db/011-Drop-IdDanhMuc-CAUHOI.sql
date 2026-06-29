-- Migration 011: Drop IdDanhMuc from CAUHOI table
-- Chạy script này để drop cột IdDanhMuc và foreign key liên quan trong CAUHOI

PRINT '=== Migration 011: Drop IdDanhMuc from CAUHOI ==='

DECLARE @ConstraintName NVARCHAR(255);

-- Tìm tên foreign key constraint trỏ tới IdDanhMuc trong CAUHOI
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
ELSE
BEGIN
    PRINT '  - No foreign key constraint found on CAUHOI(IdDanhMuc)';
END

-- Drop column IdDanhMuc nếu tồn tại
IF EXISTS (SELECT * FROM sys.columns WHERE Name='IdDanhMuc' AND Object_ID=Object_ID('CAUHOI'))
BEGIN
    ALTER TABLE CAUHOI DROP COLUMN IdDanhMuc;
    PRINT '  + Dropped column IdDanhMuc from CAUHOI';
END
ELSE
BEGIN
    PRINT '  - Column IdDanhMuc does not exist in CAUHOI';
END
GO
