namespace Employee_History.Features.Attendance
{
    /// <summary>Data access for attendance capture and reads.</summary>
    public interface IAttendanceRepository
    {
        Task<IEnumerable<AttendanceRecord>> GetHistoryAsync(DateTime? from = null, DateTime? to = null, string? staffId = null);
        Task<AttendanceRecord?> GetLatestForStaffAsync(string staffId);
        Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date);
        Task<AttendanceRecord?> GetByStaffAndDateAsync(string staffId, DateTime date);
        Task<IEnumerable<AttendanceRecord>> GetRangeAsync(DateTime startDate, DateTime endDate, string? staffId = null);
        /// <summary>Records a check-in for today. Never overwrites an existing check-in (returns AlreadyCheckedIn).</summary>
        Task<CheckinResult> CheckinAsync(string staffId, string? location, TimeSpan lateThreshold);
        /// <summary>Sets today's exit time. Returns null when the user has not checked in today.</summary>
        Task<AttendanceRecord?> CheckoutAsync(string staffId);
        Task<IEnumerable<AttendanceRecord>> GetLateCheckinsAsync();
        Task<AttendanceSummaryDto> GetSummaryAsync(DateTime date);
    }
}
