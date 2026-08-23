# ============================================================================
# Sets up a local test database on SQL Server LocalDB — run from the VS Code
# terminal:  powershell -ExecutionPolicy Bypass -File Database/setup-localdb.ps1
#
# 1. Creates database Attendance_System on (localdb)\MSSQLLocalDB
# 2. Runs 000_local_dev_schema.sql (legacy shape) + migrations/001_v2_upgrade.sql
# 3. Seeds a super-admin:  Staff_ID ADMIN001 / password Admin@123!
# ============================================================================
param(
    [string]$Instance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "Attendance_System",
    [string]$AdminStaffId = "ADMIN001",
    [string]$AdminPassword = "Admin@123!"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Invoke-SqlBatches([string]$connStr, [string]$sqlText) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        # Split on GO batch separators (line containing only GO)
        $batches = [regex]::Split($sqlText, '(?im)^\s*GO\s*$')
        foreach ($batch in $batches) {
            if ([string]::IsNullOrWhiteSpace($batch)) { continue }
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $batch
            $cmd.CommandTimeout = 120
            [void]$cmd.ExecuteNonQuery()
        }
    } finally { $conn.Close() }
}

# 1. Create the database if missing
Write-Host "Ensuring database [$Database] exists on $Instance ..."
Invoke-SqlBatches "Server=$Instance;Database=master;Trusted_Connection=True" `
    "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];"

$dbConn = "Server=$Instance;Database=$Database;Trusted_Connection=True"

# 2. Schema + migration
Write-Host "Running 000_local_dev_schema.sql ..."
Invoke-SqlBatches $dbConn (Get-Content (Join-Path $scriptDir "000_local_dev_schema.sql") -Raw)
Write-Host "Running migrations/001_v2_upgrade.sql ..."
Invoke-SqlBatches $dbConn (Get-Content (Join-Path $scriptDir "migrations\001_v2_upgrade.sql") -Raw)

# 3. Seed super-admin with a PBKDF2 hash in the backend's format
$salt = New-Object byte[] 16
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($salt)
$pbkdf2 = New-Object System.Security.Cryptography.Rfc2898DeriveBytes(
    $AdminPassword, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
$hash = [Convert]::ToBase64String($pbkdf2.GetBytes(32))
$stored = '$PBKDF2$v=3$iter=100000$' + [Convert]::ToBase64String($salt) + '$' + $hash

$conn = New-Object System.Data.SqlClient.SqlConnection($dbConn)
$conn.Open()
try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM [User] WHERE Staff_ID = @Staff_ID)
    UPDATE [User] SET Password = @Password, Lab_role = 'A1', ApprovalStatus = 1 WHERE Staff_ID = @Staff_ID;
ELSE
    INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role, Password, ApprovalStatus, ApprovalDate)
    VALUES (@Staff_ID, 'Local Admin', 'admin@local.test', 0, 'A1', @Password, 1, GETUTCDATE());
"@
    [void]$cmd.Parameters.AddWithValue("@Staff_ID", $AdminStaffId)
    [void]$cmd.Parameters.AddWithValue("@Password", $stored)
    [void]$cmd.ExecuteNonQuery()
} finally { $conn.Close() }

Write-Host ""
Write-Host "Done. Local test database is ready." -ForegroundColor Green
Write-Host "  Connection string : Server=$Instance;Database=$Database;Trusted_Connection=True;Encrypt=False"
Write-Host "  Admin login       : $AdminStaffId / $AdminPassword"
Write-Host "Point appsettings.Local.json ConnectionStrings:DefaultConnection at it and 'dotnet run'."
