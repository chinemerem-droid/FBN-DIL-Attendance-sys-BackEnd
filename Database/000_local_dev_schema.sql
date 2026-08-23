-- ============================================================================
-- 000 — Local development schema (LocalDB / fresh SQL Server)
--
-- Creates the LEGACY (pre-v2) shape of the database, matching production
-- before migration 001. After running this, run migrations/001_v2_upgrade.sql
-- — exactly what production needs — so local dev also validates the migration.
--
-- Idempotent: safe to run more than once.
-- ============================================================================

IF OBJECT_ID('dbo.[User]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User] (
        Staff_ID NVARCHAR(50) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NULL,
        Email NVARCHAR(256) NULL,
        Phone_number BIGINT NULL,
        Lab_role NVARCHAR(10) NULL,
        Password NVARCHAR(400) NULL,
        ApprovalStatus BIT NOT NULL DEFAULT 0,
        ApprovalDate DATETIME2 NULL,
        RemovalDate DATETIME2 NULL,
        DeviceID NVARCHAR(200) NULL,
        DeviceModel NVARCHAR(200) NULL
    );
    PRINT 'Created [User]';
END
GO

IF OBJECT_ID('dbo.Attendance_History', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Attendance_History (
        Staff_ID NVARCHAR(50) NOT NULL,
        EntryTime TIME NULL,
        ExitTime TIME NULL,
        Date DATETIME NOT NULL,
        CheckinStatus NVARCHAR(20) NULL
    );
    PRINT 'Created Attendance_History';
END
GO

IF OBJECT_ID('dbo.[Notification]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Notification] (
        Staff_ID NVARCHAR(50) NOT NULL,
        Message NVARCHAR(400) NULL,
        RoleID NVARCHAR(10) NULL,
        IsRead BIT NOT NULL DEFAULT 0
    );
    PRINT 'Created [Notification]';
END
GO

IF OBJECT_ID('dbo.PasswordResetTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Staff_ID NVARCHAR(50) NOT NULL,
        Token NVARCHAR(10) NOT NULL, -- legacy width on purpose; migration 001 widens it
        ExpiryDate DATETIME2 NOT NULL
    );
    PRINT 'Created PasswordResetTokens';
END
GO

IF OBJECT_ID('dbo.LeaveRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Staff_ID NVARCHAR(50) NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
    );
    PRINT 'Created LeaveRequests';
END
GO

IF OBJECT_ID('dbo.Images', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Images (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Staff_ID NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(260) NULL,
        FileType NVARCHAR(100) NULL,
        FileSize BIGINT NULL,
        ImageData VARBINARY(MAX) NULL
    );
    PRINT 'Created Images';
END
GO

-- ----------------------------------------------------------------------------
-- Stored procedures the API depends on (Leave + Image features)
-- ----------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.InsertLeaveRequest
    @Staff_ID NVARCHAR(50), @StartDate DATETIME2, @EndDate DATETIME2
AS
BEGIN
    INSERT INTO dbo.LeaveRequests (Staff_ID, StartDate, EndDate, Status)
    VALUES (@Staff_ID, @StartDate, @EndDate, 'Pending');
END
GO

CREATE OR ALTER PROCEDURE dbo.GetLeaveRequests
AS
BEGIN
    SELECT Id, Staff_ID, StartDate, EndDate, Status FROM dbo.LeaveRequests ORDER BY Id DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.ApproveLeaveRequest
    @Staff_ID NVARCHAR(50)
AS
BEGIN
    UPDATE dbo.LeaveRequests SET Status = 'Approved'
    WHERE Staff_ID = @Staff_ID AND Status = 'Pending';
END
GO

CREATE OR ALTER PROCEDURE dbo.InsertImage
    @FileName NVARCHAR(260), @FileType NVARCHAR(100), @FileSize BIGINT,
    @ImageData VARBINARY(MAX), @Staff_ID NVARCHAR(50)
AS
BEGIN
    -- One image per staff member: replace on re-upload.
    DELETE FROM dbo.Images WHERE Staff_ID = @Staff_ID;
    INSERT INTO dbo.Images (Staff_ID, FileName, FileType, FileSize, ImageData)
    VALUES (@Staff_ID, @FileName, @FileType, @FileSize, @ImageData);
END
GO

CREATE OR ALTER PROCEDURE dbo.GetImageById
    @Staff_ID NVARCHAR(50)
AS
BEGIN
    SELECT TOP 1 ImageData FROM dbo.Images WHERE Staff_ID = @Staff_ID;
END
GO

PRINT 'Local dev schema (legacy shape) ready. Now run migrations/001_v2_upgrade.sql';
GO
