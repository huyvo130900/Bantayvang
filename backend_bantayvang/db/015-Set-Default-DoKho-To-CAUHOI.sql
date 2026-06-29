-- === Migration 015: Set Default DoKho to CAUHOI ===

-- Update any existing NULL or empty values to '1' (Dễ)
UPDATE CAUHOI SET DoKho = '1' WHERE DoKho IS NULL OR LTRIM(RTRIM(DoKho)) = '';

-- Add DEFAULT constraint to DoKho column if it doesn't already have one
IF NOT EXISTS (
    SELECT * FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID('CAUHOI') 
      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('CAUHOI'), 'DoKho', 'ColumnId')
)
BEGIN
    ALTER TABLE CAUHOI ADD CONSTRAINT DF_CAUHOI_DoKho DEFAULT '1' FOR DoKho;
    PRINT 'Added DEFAULT constraint for DoKho in CAUHOI';
END
ELSE
BEGIN
    PRINT 'DEFAULT constraint for DoKho in CAUHOI already exists';
END
GO
