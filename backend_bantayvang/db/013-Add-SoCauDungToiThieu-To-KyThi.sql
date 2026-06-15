-- === Migration 013: Add SoCauDungToiThieu to KyThi ===
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name='SoCauDungToiThieu' AND Object_ID=Object_ID('KyThi'))
BEGIN
    ALTER TABLE KyThi ADD SoCauDungToiThieu INT NULL;
    PRINT 'Added SoCauDungToiThieu column to KyThi';
END
GO
