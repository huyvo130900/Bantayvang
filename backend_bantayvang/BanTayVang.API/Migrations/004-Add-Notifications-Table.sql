USE HeThongBanTayVang;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    CREATE TABLE Notifications (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NULL,
        Title nvarchar(255) NOT NULL,
        Message nvarchar(1000) NOT NULL,
        Type nvarchar(50) NOT NULL DEFAULT 'Info',
        IsRead bit NOT NULL DEFAULT 0,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ReadAt datetime2 NULL,
        RelatedUrl nvarchar(500) NULL,
        
        CONSTRAINT FK_Notifications_UserId FOREIGN KEY (UserId)
            REFERENCES TAIKHOAN(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
    CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);
    
    PRINT 'Notifications table created successfully';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists';
END
GO