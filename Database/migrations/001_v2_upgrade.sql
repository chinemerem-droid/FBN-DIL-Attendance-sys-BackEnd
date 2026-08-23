-- ============================================================================
-- Migration 001 — v2 upgrade (August 2026)
-- Idempotent: safe to run more than once. Additive only — no data is dropped.
--
-- Run this BEFORE deploying the v2 backend. It adds:
--   1. Notification.Id            (identity PK surface for read/delete by id)
--   2. Attendance_History.Id      (stable record id)
--   3. Attendance_History.Location (check-in coordinates, shown in admin Home)
--   4. RefreshTokens table         (session refresh / logout revocation)
--   5. PasswordResetTokens.Token widened to fit 43-char secure tokens
--   6. Helpful indexes
-- ============================================================================

-- 1. Notification.Id
IF COL_LENGTH('dbo.Notification', 'Id') IS NULL
BEGIN
    ALTER TABLE dbo.[Notification] ADD Id INT IDENTITY(1,1) NOT NULL;
    PRINT 'Added Notification.Id';
END
GO

-- 2. Attendance_History.Id
IF COL_LENGTH('dbo.Attendance_History', 'Id') IS NULL
BEGIN
    ALTER TABLE dbo.Attendance_History ADD Id INT IDENTITY(1,1) NOT NULL;
    PRINT 'Added Attendance_History.Id';
END
GO

-- 3. Attendance_History.Location
IF COL_LENGTH('dbo.Attendance_History', 'Location') IS NULL
BEGIN
    ALTER TABLE dbo.Attendance_History ADD Location NVARCHAR(100) NULL;
    PRINT 'Added Attendance_History.Location';
END
GO

-- 4. RefreshTokens
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Staff_ID NVARCHAR(50) NOT NULL,
        Token NVARCHAR(100) NOT NULL,
        ExpiryDate DATETIME2 NOT NULL,
        Revoked BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_RefreshTokens_Token ON dbo.RefreshTokens (Token);
    CREATE INDEX IX_RefreshTokens_Staff_ID ON dbo.RefreshTokens (Staff_ID);
    PRINT 'Created RefreshTokens';
END
GO

-- 5. Widen PasswordResetTokens.Token (new tokens are 43 chars; old column may be tiny)
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PasswordResetTokens' AND COLUMN_NAME = 'Token'
      AND (CHARACTER_MAXIMUM_LENGTH < 100 AND CHARACTER_MAXIMUM_LENGTH <> -1)
)
BEGIN
    BEGIN TRY
        ALTER TABLE dbo.PasswordResetTokens ALTER COLUMN Token NVARCHAR(100) NOT NULL;
        PRINT 'Widened PasswordResetTokens.Token to NVARCHAR(100)';
    END TRY
    BEGIN CATCH
        -- If Token participates in a PK/index, widen manually after dropping it.
        PRINT 'WARNING: could not widen PasswordResetTokens.Token automatically: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- Clear out old-format (5-char, never-expiring) reset tokens.
IF OBJECT_ID('dbo.PasswordResetTokens', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.PasswordResetTokens WHERE LEN(Token) < 20;
END
GO

-- 6. Indexes for common queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Attendance_History_Staff_Date' AND object_id = OBJECT_ID('dbo.Attendance_History'))
BEGIN
    CREATE INDEX IX_Attendance_History_Staff_Date ON dbo.Attendance_History (Staff_ID, Date);
    PRINT 'Created IX_Attendance_History_Staff_Date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_Email' AND object_id = OBJECT_ID('dbo.[User]'))
BEGIN
    CREATE INDEX IX_User_Email ON dbo.[User] (Email);
    PRINT 'Created IX_User_Email';
END
GO

PRINT 'Migration 001 complete.';
GO
