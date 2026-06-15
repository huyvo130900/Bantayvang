-- Migration: Xóa cột DiemDat khỏi bảng DETHI
DECLARE @ConstraintName NVARCHAR(256)
SELECT @ConstraintName = obj.name
FROM sys.columns col
JOIN sys.objects obj ON col.default_object_id = obj.object_id
WHERE col.object_id = OBJECT_ID('DETHI') AND col.name = 'DiemDat'

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE DETHI DROP CONSTRAINT ' + @ConstraintName)
    PRINT 'Dropped default constraint ' + @ConstraintName
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DETHI' AND COLUMN_NAME = 'DiemDat')
BEGIN
    ALTER TABLE DETHI DROP COLUMN DiemDat;
    PRINT 'Removed DiemDat column from DETHI';
END
ELSE
BEGIN
    PRINT 'DiemDat column does not exist in DETHI';
END
