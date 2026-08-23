using Dapper;
using Microsoft.Data.SqlClient;

namespace Employee_History.Features.Attendance
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private const string SelectColumns = @"
            a.Id, a.Staff_ID, u.Name, a.EntryTime, a.ExitTime, a.Date, a.Location, a.CheckinStatus";

        private const string FromClause = @"
            FROM Attendance_History a
            LEFT JOIN [User] u ON u.Staff_ID = a.Staff_ID";

        private readonly SqlConnection _connection;

        public AttendanceRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<AttendanceRecord>> GetHistoryAsync(DateTime? from = null, DateTime? to = null, string? staffId = null)
        {
            var sql = $@"
                SELECT {SelectColumns}
                {FromClause}
                WHERE (@From IS NULL OR a.Date >= @From)
                  AND (@To IS NULL OR a.Date <= @To)
                  AND (@Staff_ID IS NULL OR a.Staff_ID = @Staff_ID)
                ORDER BY a.Date DESC, a.EntryTime DESC";

            return await _connection.QueryAsync<AttendanceRecord>(sql, new { From = from, To = to, Staff_ID = staffId });
        }

        public async Task<AttendanceRecord?> GetLatestForStaffAsync(string staffId)
        {
            var sql = $@"
                SELECT TOP 1 {SelectColumns}
                {FromClause}
                WHERE a.Staff_ID = @Staff_ID
                ORDER BY a.Date DESC";
            return await _connection.QueryFirstOrDefaultAsync<AttendanceRecord>(sql, new { Staff_ID = staffId });
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date)
        {
            var sql = $@"
                SELECT {SelectColumns}
                {FromClause}
                WHERE CAST(a.Date AS DATE) = CAST(@Date AS DATE)
                ORDER BY a.EntryTime";
            return await _connection.QueryAsync<AttendanceRecord>(sql, new { Date = date });
        }

        public async Task<AttendanceRecord?> GetByStaffAndDateAsync(string staffId, DateTime date)
        {
            var sql = $@"
                SELECT {SelectColumns}
                {FromClause}
                WHERE a.Staff_ID = @Staff_ID AND CAST(a.Date AS DATE) = CAST(@Date AS DATE)";
            return await _connection.QueryFirstOrDefaultAsync<AttendanceRecord>(sql, new { Staff_ID = staffId, Date = date });
        }

        public async Task<IEnumerable<AttendanceRecord>> GetRangeAsync(DateTime startDate, DateTime endDate, string? staffId = null)
        {
            var sql = $@"
                SELECT {SelectColumns}
                {FromClause}
                WHERE a.Date >= @StartDate AND a.Date <= @EndDate
                  AND (@Staff_ID IS NULL OR a.Staff_ID = @Staff_ID)
                ORDER BY a.Date";
            return await _connection.QueryAsync<AttendanceRecord>(sql, new { StartDate = startDate, EndDate = endDate, Staff_ID = staffId });
        }

        public async Task<CheckinResult> CheckinAsync(string staffId, string? location, TimeSpan lateThreshold)
        {
            var now = DateTime.Now;
            var currentDate = now.Date;
            // Whole seconds only, so times serialize as "HH:mm:ss".
            var entryTime = new TimeSpan(now.TimeOfDay.Hours, now.TimeOfDay.Minutes, now.TimeOfDay.Seconds);

            var existingEntry = await _connection.QueryFirstOrDefaultAsync<AttendanceRecord>(
                @"SELECT Id, Staff_ID, EntryTime, ExitTime, Date, Location, CheckinStatus
                  FROM Attendance_History
                  WHERE Staff_ID = @Staff_ID AND CAST(Date AS DATE) = @currentDate",
                new { Staff_ID = staffId, currentDate });

            if (existingEntry != null && existingEntry.EntryTime.HasValue)
            {
                // Never overwrite an existing check-in.
                return new CheckinResult { Outcome = CheckinOutcome.AlreadyCheckedIn, Record = existingEntry };
            }

            string status = entryTime > lateThreshold ? "LATE" : "ON TIME";

            await _connection.ExecuteAsync(
                @"INSERT INTO Attendance_History (Staff_ID, EntryTime, ExitTime, Date, CheckinStatus, Location)
                  VALUES (@Staff_ID, @entryTime, NULL, @currentDate, @status, @location)",
                new { Staff_ID = staffId, entryTime, currentDate, status, location });

            var newEntry = await _connection.QueryFirstOrDefaultAsync<AttendanceRecord>(
                @"SELECT Id, Staff_ID, EntryTime, ExitTime, Date, Location, CheckinStatus
                  FROM Attendance_History
                  WHERE Staff_ID = @Staff_ID AND CAST(Date AS DATE) = @currentDate",
                new { Staff_ID = staffId, currentDate });

            return new CheckinResult
            {
                Outcome = newEntry != null ? CheckinOutcome.Created : CheckinOutcome.Failed,
                Record = newEntry
            };
        }

        public async Task<AttendanceRecord?> CheckoutAsync(string staffId)
        {
            var now = DateTime.Now;
            var currentDate = now.Date;
            var exitTime = new TimeSpan(now.TimeOfDay.Hours, now.TimeOfDay.Minutes, now.TimeOfDay.Seconds);

            var rowsAffected = await _connection.ExecuteAsync(
                @"UPDATE Attendance_History
                  SET ExitTime = @exitTime
                  WHERE Staff_ID = @staffId AND CAST(Date AS DATE) = @currentDate AND EntryTime IS NOT NULL",
                new { staffId, currentDate, exitTime });

            if (rowsAffected == 0)
            {
                return null; // Not checked in today.
            }

            return await _connection.QueryFirstOrDefaultAsync<AttendanceRecord>(
                @"SELECT Id, Staff_ID, EntryTime, ExitTime, Date, Location, CheckinStatus
                  FROM Attendance_History
                  WHERE Staff_ID = @staffId AND CAST(Date AS DATE) = @currentDate",
                new { staffId, currentDate });
        }

        public async Task<IEnumerable<AttendanceRecord>> GetLateCheckinsAsync()
        {
            var sql = $@"
                SELECT {SelectColumns}
                {FromClause}
                WHERE a.CheckinStatus = 'LATE'
                ORDER BY a.Date DESC";
            return await _connection.QueryAsync<AttendanceRecord>(sql);
        }

        public async Task<AttendanceSummaryDto> GetSummaryAsync(DateTime date)
        {
            var sql = @"
                SELECT
                    (SELECT COUNT(*) FROM [User] WHERE ApprovalStatus = 1) AS TotalEmployees,
                    COUNT(*) AS Present,
                    ISNULL(SUM(CASE WHEN ExitTime IS NOT NULL THEN 1 ELSE 0 END), 0) AS CheckedOut,
                    ISNULL(SUM(CASE WHEN CheckinStatus = 'LATE' THEN 1 ELSE 0 END), 0) AS Late
                FROM Attendance_History
                WHERE CAST(Date AS DATE) = CAST(@Date AS DATE) AND EntryTime IS NOT NULL";

            var summary = await _connection.QueryFirstAsync<AttendanceSummaryDto>(sql, new { Date = date });
            summary.Date = date.Date;
            return summary;
        }
    }
}
