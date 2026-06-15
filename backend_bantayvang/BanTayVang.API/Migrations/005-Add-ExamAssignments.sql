USE HeThongBanTayVang;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExamAssignments' AND xtype='U')
BEGIN
    CREATE TABLE ExamAssignments (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ExamId int NOT NULL,
        UserId int NOT NULL,
        AssignedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        AssignedBy int NULL,
        CustomStartTime datetime2 NULL,
        ExtraMinutes int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        Note nvarchar(500) NULL,
        CONSTRAINT FK_ExamAssignments_Exam FOREIGN KEY (ExamId) REFERENCES DETHI(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ExamAssignments_User FOREIGN KEY (UserId) REFERENCES TAIKHOAN(Id),
        CONSTRAINT UQ_ExamAssignments_ExamUser UNIQUE (ExamId, UserId)
    );
    CREATE INDEX IX_ExamAssignments_UserId ON ExamAssignments(UserId);
    PRINT 'ExamAssignments table created';
END
ELSE PRINT 'ExamAssignments already exists';
GO