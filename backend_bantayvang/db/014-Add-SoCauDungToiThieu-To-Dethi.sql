-- === Migration 014: Add SoCauDungToiThieu to Dethi ===
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='SoCauDungToiThieu' AND Object_ID=Object_ID('DETHI'))
BEGIN
    ALTER TABLE DETHI ADD SoCauDungToiThieu INT NULL;
    PRINT 'Added SoCauDungToiThieu column to DETHI';
END
GO
