using Employee_History.Common;
using Employee_History.Common.Models;
using Employee_History.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Attendance
{
    /// <summary>
    /// Attendance capture and reporting. Capture: mobile self check-in
    /// (device + geofence validated, legacy route /api/User/checkin), admin
    /// check-in, and check-out. Reads: full/filtered history, by-date,
    /// by-staff, date ranges, late arrivals, and a daily summary.
    /// Staff can only read/write their own records; admins (A1/B2) see everyone.
    /// </summary>
    [Route("api/Attendance")]
    [Authorize]
    public class AttendanceController : ApiControllerBase
    {
        private readonly IAttendanceRepository _attendance;
        private readonly IUserRepository _users;
        private readonly IConfiguration _configuration;

        public AttendanceController(IAttendanceRepository attendance, IUserRepository users, IConfiguration configuration)
        {
            _attendance = attendance;
            _users = users;
            _configuration = configuration;
        }

        private bool CanAccess(string? staffId) => CallerIsAdmin || (staffId != null && staffId == CallerStaffId);

        private TimeSpan LateThreshold =>
            TimeSpan.TryParse(_configuration["Attendance:LateThreshold"], out var t) ? t : new TimeSpan(11, 0, 0);

        // ------------------------------------------------------------------
        // Capture
        // ------------------------------------------------------------------

        /// <summary>Mobile self check-in. Validates the caller's registered device and the office geofence, then records today's arrival.</summary>
        /// <remarks>Expects: bearer token + { deviceID, deviceModel, latitude, longitude } (staff_ID optional — admins may check in others). Returns: 200 with the attendance record; 401 on device mismatch; 400 outside the geofence; 409 if already checked in today.</remarks>
        [HttpPost("/api/User/checkin")]
        public async Task<IActionResult> Checkin([FromBody] CheckinRequest request)
        {
            var staffId = CallerStaffId;
            if (staffId == null)
            {
                return Unauthorized(new ApiMessage("Invalid token.", false));
            }

            // A staff member can only check themselves in.
            if (!string.IsNullOrEmpty(request.Staff_ID) && request.Staff_ID != staffId && !CallerIsAdmin)
            {
                return Forbid();
            }
            var targetStaffId = CallerIsAdmin && !string.IsNullOrEmpty(request.Staff_ID) ? request.Staff_ID : staffId;

            var user = await _users.GetByStaffIdAsync(targetStaffId);
            if (user == null)
            {
                return NotFound(new ApiMessage("Invalid Staff ID.", false));
            }

            if (!CallerIsAdmin)
            {
                if (string.IsNullOrEmpty(user.DeviceID) || user.DeviceID != request.DeviceID || user.DeviceModel != request.DeviceModel)
                {
                    return Unauthorized(new ApiMessage("Device information does not match.", false));
                }

                if (!IsLocationInRange(request.Longitude, request.Latitude))
                {
                    return BadRequest(new ApiMessage("Location is not within acceptable range.", false));
                }
            }

            var location = request.Latitude.HasValue && request.Longitude.HasValue
                ? $"{request.Latitude},{request.Longitude}"
                : null;

            var result = await _attendance.CheckinAsync(targetStaffId, location, LateThreshold);
            return ToCheckinResponse(result);
        }

        /// <summary>Admin/kiosk check-in for any staff member. Skips device and geofence checks.</summary>
        /// <remarks>Expects (Admin): { staff_ID, location? }. Returns: 200 with the attendance record; 409 if already checked in today.</remarks>
        [Authorize(Policy = "Admin")]
        [HttpPost("CheckIn")]
        public async Task<IActionResult> AdminCheckIn([FromBody] AdminCheckinRequest request)
        {
            var result = await _attendance.CheckinAsync(request.Staff_ID, request.Location, LateThreshold);
            return ToCheckinResponse(result);
        }

        /// <summary>Records today's departure (sets exitTime). Staff check themselves out; admins may check out anyone.</summary>
        /// <remarks>Expects: bearer token + { staff_ID? }. Returns: 200 with the updated record; 409 if there is no check-in today.</remarks>
        [HttpPut("Checkout")]
        [HttpPost("CheckOut")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var targetStaffId = CallerIsAdmin && !string.IsNullOrEmpty(request.Staff_ID)
                ? request.Staff_ID
                : CallerStaffId;

            if (string.IsNullOrEmpty(targetStaffId))
            {
                return BadRequest(new ApiMessage("Staff ID is required.", false));
            }
            if (!CanAccess(targetStaffId))
            {
                return Forbid();
            }

            var record = await _attendance.CheckoutAsync(targetStaffId);
            if (record == null)
            {
                return Conflict(new ApiMessage("Cannot check out: no check-in found for today.", false));
            }
            return Ok(record);
        }

        // ------------------------------------------------------------------
        // Reads
        // ------------------------------------------------------------------

        /// <summary>Full attendance history, optionally filtered. Non-admins always get only their own records.</summary>
        /// <remarks>Expects: bearer token; optional ?from=&amp;to=&amp;staffId=. Returns: 200 with AttendanceRecord[] (newest first).</remarks>
        [HttpGet("AttendanceHistory")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? staffId = null)
        {
            if (!CallerIsAdmin)
            {
                staffId = CallerStaffId;
            }

            return Ok(await _attendance.GetHistoryAsync(from, to, staffId));
        }

        /// <summary>All attendance for one day (the admin Home page poll).</summary>
        /// <remarks>Expects (Admin): { date: "YYYY-MM-DD" }. Returns: 200 with AttendanceRecord[] ordered by entry time.</remarks>
        [Authorize(Policy = "Admin")]
        [HttpPost("AttendanceByDate")]
        public async Task<IActionResult> GetByDate([FromBody] AttendanceDateRequest request)
        {
            return Ok(await _attendance.GetByDateAsync(request.Date));
        }

        /// <summary>Daily headcount summary for the dashboard.</summary>
        /// <remarks>Expects (Admin): optional ?date= (defaults to today). Returns: 200 { date, totalEmployees, present, checkedOut, late }.</remarks>
        [Authorize(Policy = "Admin")]
        [HttpGet("Summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? date = null)
        {
            return Ok(await _attendance.GetSummaryAsync(date ?? DateTime.Now.Date));
        }

        /// <summary>The most recent attendance record for one staff member.</summary>
        /// <remarks>Expects: bearer token + { staff_ID } (own id unless admin). Returns: 200 with the record; 404 when none exists.</remarks>
        [HttpPost("AttendanceByID")]
        public async Task<IActionResult> GetByStaff([FromBody] StaffAttendanceRequest request)
        {
            if (!CanAccess(request.Staff_ID))
            {
                return Forbid();
            }

            var response = await _attendance.GetLatestForStaffAsync(request.Staff_ID);
            if (response == null)
            {
                return NotFound(new ApiMessage("No attendance record found.", false));
            }
            return Ok(response);
        }

        /// <summary>One staff member's attendance on one day.</summary>
        /// <remarks>Expects: bearer token + { staff_ID, date } (own id unless admin). Returns: 200 with the record; 404 when none exists.</remarks>
        [HttpPost("GetAttendanceByIDandDate")]
        public async Task<IActionResult> GetByStaffAndDate([FromBody] StaffAttendanceRequest request)
        {
            if (!CanAccess(request.Staff_ID))
            {
                return Forbid();
            }

            var response = await _attendance.GetByStaffAndDateAsync(request.Staff_ID, request.Date);
            if (response == null)
            {
                return NotFound(new ApiMessage("No attendance record found.", false));
            }
            return Ok(response);
        }

        /// <summary>One staff member's attendance between two dates.</summary>
        /// <remarks>Expects: bearer token + { staff_ID, startDate, endDate } (own id unless admin). Returns: 200 with AttendanceRecord[].</remarks>
        [HttpPost("GetAttendanceByIDbtwDates")]
        public async Task<IActionResult> GetByStaffBetweenDates([FromBody] DateRangeRequest request)
        {
            if (string.IsNullOrEmpty(request.Staff_ID))
            {
                return BadRequest(new ApiMessage("Staff ID is required.", false));
            }
            if (!CanAccess(request.Staff_ID))
            {
                return Forbid();
            }

            return Ok(await _attendance.GetRangeAsync(request.StartDate, request.EndDate, request.Staff_ID));
        }

        /// <summary>All attendance between two dates.</summary>
        /// <remarks>Expects (Admin): { startDate, endDate }. Returns: 200 with AttendanceRecord[].</remarks>
        [Authorize(Policy = "Admin")]
        [HttpPost("GetAttendancebtwDates")]
        public async Task<IActionResult> GetBetweenDates([FromBody] DateRangeRequest request)
        {
            return Ok(await _attendance.GetRangeAsync(request.StartDate, request.EndDate));
        }

        /// <summary>All late check-ins (checkinStatus = "LATE"), newest first.</summary>
        /// <remarks>Expects (Admin): bearer token only. Returns: 200 with AttendanceRecord[].</remarks>
        [Authorize(Policy = "Admin")]
        [HttpGet("Latecheckin")]
        [HttpPost("Latecheckin")]
        public async Task<IActionResult> GetLateCheckins()
        {
            return Ok(await _attendance.GetLateCheckinsAsync());
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IActionResult ToCheckinResponse(CheckinResult result) => result.Outcome switch
        {
            CheckinOutcome.AlreadyCheckedIn => Conflict(new ApiMessage("Already checked in today.", false)),
            CheckinOutcome.Created => Ok(result.Record),
            _ => StatusCode(500, new ApiMessage("Failed to record check-in. Please try again.", false))
        };

        private bool IsLocationInRange(decimal? longitude, decimal? latitude)
        {
            if (!longitude.HasValue || !latitude.HasValue)
            {
                return false;
            }

            var minLongitude = _configuration.GetValue<decimal>("LocationRange:MinLongitude");
            var maxLongitude = _configuration.GetValue<decimal>("LocationRange:MaxLongitude");
            var minLatitude = _configuration.GetValue<decimal>("LocationRange:MinLatitude");
            var maxLatitude = _configuration.GetValue<decimal>("LocationRange:MaxLatitude");

            return longitude >= minLongitude && longitude <= maxLongitude &&
                   latitude >= minLatitude && latitude <= maxLatitude;
        }
    }
}
