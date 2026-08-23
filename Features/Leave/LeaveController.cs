using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Leave
{
    /// <summary>
    /// Leave management: staff submit leave requests for a date range; admins
    /// list pending requests and approve them. Staff can only request leave
    /// for themselves.
    /// </summary>
    [Route("api/Leave")]
    [Authorize]
    public class LeaveController : ApiControllerBase
    {
        private readonly ILeaveRepository _leave;

        public LeaveController(ILeaveRepository leave)
        {
            _leave = leave;
        }

        /// <summary>Submits a leave request for a date range.</summary>
        /// <remarks>Expects: bearer token + { startDate, endDate } (staff_ID optional — admins may submit for others). Returns: 200 { success, message }; 400 when endDate is before startDate.</remarks>
        [HttpPost("request")]
        public async Task<IActionResult> RequestLeave([FromBody] CreateLeaveRequest request)
        {
            var staffId = CallerIsAdmin && !string.IsNullOrEmpty(request.Staff_ID) ? request.Staff_ID : CallerStaffId;
            if (string.IsNullOrEmpty(staffId))
            {
                return BadRequest(new ApiMessage("Staff ID is required.", false));
            }
            if (request.EndDate < request.StartDate)
            {
                return BadRequest(new ApiMessage("End date cannot be before start date.", false));
            }

            await _leave.RequestLeaveAsync(staffId, request.StartDate, request.EndDate);
            return Ok(new ApiMessage("Leave request submitted successfully"));
        }

        /// <summary>Lists all leave requests.</summary>
        /// <remarks>Expects (Admin): bearer token only. Returns: 200 with [{ id, staff_ID, startDate, endDate, status }].</remarks>
        [Authorize(Policy = "Admin")]
        [HttpGet("Getrequests")]
        public async Task<IActionResult> GetLeaveRequests()
        {
            return Ok(await _leave.GetLeaveRequestsAsync());
        }

        /// <summary>Approves a staff member's pending leave request.</summary>
        /// <remarks>Expects (Admin): { staff_ID }. Returns: 200 { success, message }.</remarks>
        [Authorize(Policy = "Admin")]
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLeaveRequest([FromBody] Users.StaffIdRequest request)
        {
            await _leave.ApproveLeaveRequestAsync(request.Staff_ID);
            return Ok(new ApiMessage("Leave request approved successfully"));
        }
    }
}
