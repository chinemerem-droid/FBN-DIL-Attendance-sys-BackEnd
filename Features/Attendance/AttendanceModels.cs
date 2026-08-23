using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Employee_History.Features.Attendance
{
    // ------------------------------------------------------------------
    // Entity / response  (camelCase on the wire: staff_ID, entryTime, ...)
    // ------------------------------------------------------------------

    /// <summary>
    /// One attendance row: <c>{ id, staff_ID, name, entryTime "HH:mm:ss",
    /// exitTime, date ISO, location "lat,long", checkinStatus "ON TIME"|"LATE" }</c>.
    /// </summary>
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public string Staff_ID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public TimeSpan? EntryTime { get; set; }
        public TimeSpan? ExitTime { get; set; }
        public DateTime Date { get; set; }
        public string? Location { get; set; }
        public string CheckinStatus { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EntryTimeString => EntryTime?.ToString(@"hh\:mm\:ss");

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExitTimeString => ExitTime?.ToString(@"hh\:mm\:ss");
    }

    /// <summary>Daily summary: <c>{ date, totalEmployees, present, checkedOut, late }</c>.</summary>
    public class AttendanceSummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalEmployees { get; set; }
        public int Present { get; set; }
        public int CheckedOut { get; set; }
        public int Late { get; set; }
    }

    public enum CheckinOutcome
    {
        Created,
        AlreadyCheckedIn,
        Failed
    }

    public class CheckinResult
    {
        public CheckinOutcome Outcome { get; set; }
        public AttendanceRecord? Record { get; set; }
    }

    // ------------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------------

    /// <summary>Body for the mobile self check-in (device + geofence validated).</summary>
    public class CheckinRequest
    {
        /// <summary>Optional — defaults to the caller's own staff id from the token.</summary>
        public string? Staff_ID { get; set; }

        [Required]
        public string DeviceID { get; set; } = string.Empty;

        [Required]
        public string DeviceModel { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    /// <summary>Body for the admin/kiosk check-in (no device or geofence check).</summary>
    public class AdminCheckinRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        /// <summary>Free-form location label or "lat,long".</summary>
        public string? Location { get; set; }
    }

    /// <summary>Body for check-out. Staff omit staff_ID (their own is used); admins may target anyone.</summary>
    public class CheckoutRequest
    {
        public string? Staff_ID { get; set; }
    }

    /// <summary>Body for querying one day of attendance.</summary>
    public class AttendanceDateRequest
    {
        [Required]
        public DateTime Date { get; set; }
    }

    /// <summary>Body for querying attendance by staff id (optionally with a date).</summary>
    public class StaffAttendanceRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }

    /// <summary>Body for querying a date range, optionally scoped to one staff member.</summary>
    public class DateRangeRequest
    {
        public string? Staff_ID { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
