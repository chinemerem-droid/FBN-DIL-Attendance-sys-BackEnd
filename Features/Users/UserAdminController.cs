using Employee_History.Common;
using Employee_History.Common.Models;
using Employee_History.Features.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Users
{
    /// <summary>
    /// Administrative user management for the admin portal: create users,
    /// approve/deny pending registrations, list and search users, remove
    /// (offboard) users, reset device bindings, and view approval/removal
    /// history. Every endpoint requires an admin bearer token — sub admins
    /// (B2) can manage and view; approve/deny/remove are super admin (A1)
    /// only. Self-service endpoints live in <see cref="UsersController"/>.
    /// </summary>
    [Route("api/User")]
    [Authorize(Policy = "Admin")]
    public class UserAdminController : ApiControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IEmailService _emailService;

        public UserAdminController(IUserRepository users, IEmailService emailService)
        {
            _users = users;
            _emailService = emailService;
        }

        // ------------------------------------------------------------------
        // Create & list
        // ------------------------------------------------------------------

        /// <summary>Creates a new user. A1/B2 users receive a generated initial password by email; the new user appears in the A1 approval queue.</summary>
        /// <remarks>Expects (Admin): { staff_ID, name, email, phone_number, lab_role }. Returns: 200 { success, message, staff_ID }; 400 on invalid input; 409 if the staff id already exists.</remarks>
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
        {
            try
            {
                await _users.AddUserAsync(request);
                return Ok(new AddUserResponse { Success = true, Message = "User added successfully", Staff_ID = request.Staff_ID });
            }
            catch (DuplicateStaffIdException ex)
            {
                return Conflict(new ApiMessage(ex.Message, false));
            }
        }

        /// <summary>Lists all users, or a page of users when query/page/pageSize are supplied.</summary>
        /// <remarks>Expects (Admin): optional ?query=&amp;page=&amp;pageSize=. Returns: 200 with UserDto[] (no params) or { data, totalCount, page, pageSize }.</remarks>
        [HttpGet("AddedUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] string? query = null, [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {
            // Without paging params, return the plain array the current frontend expects.
            if (page == null && pageSize == null && string.IsNullOrEmpty(query))
            {
                return Ok(await _users.GetAllAsync());
            }

            var result = await _users.GetPagedAsync(
                string.IsNullOrWhiteSpace(query) ? null : query,
                Math.Max(page ?? 1, 1),
                Math.Clamp(pageSize ?? 50, 1, 200));
            return Ok(result);
        }

        /// <summary>Lists users still waiting for approval (excluding removed users).</summary>
        /// <remarks>Expects (Admin): bearer token only. Returns: 200 with UserDto[].</remarks>
        [HttpGet("nonapproved")]
        public async Task<IActionResult> GetNonApprovedUsers()
        {
            return Ok(await _users.GetNonApprovedAsync());
        }

        /// <summary>Lists users with a given role.</summary>
        /// <remarks>Expects (Admin): ?Lab_role=A1|B2|C3. Returns: 200 with UserDto[].</remarks>
        [HttpGet("employeesByRole")]
        public async Task<IActionResult> GetEmployeesByRole([FromQuery] string Lab_role)
        {
            if (string.IsNullOrEmpty(Lab_role))
            {
                return BadRequest(new ApiMessage("Lab_role is required.", false));
            }
            return Ok(await _users.GetByRoleAsync(Lab_role));
        }

        // ------------------------------------------------------------------
        // Approve / deny / remove (super admin only)
        // ------------------------------------------------------------------

        /// <summary>Approves a pending user and emails them (email failure does not undo the approval).</summary>
        /// <remarks>Expects (SuperAdmin): { staff_ID }. Returns: 200 { success, message }; 404 if the user does not exist.</remarks>
        [Authorize(Policy = "SuperAdmin")]
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveUser([FromBody] StaffIdRequest request)
        {
            var user = await _users.GetByStaffIdAsync(request.Staff_ID);
            if (user == null)
            {
                return NotFound(new ApiMessage("User not found.", false));
            }

            await _users.ApproveAsync(request.Staff_ID);

            if (!string.IsNullOrEmpty(user.Email))
            {
                _emailService.TrySendEmail(new EmailMessage
                {
                    To = user.Email,
                    Subject = "Approval",
                    Body = "You have been approved for the AMS web system."
                });
            }

            return Ok(new ApiMessage("User approval status updated successfully."));
        }

        /// <summary>Denies (rejects) a pending registration and deletes the user row.</summary>
        /// <remarks>Expects (SuperAdmin): { staff_ID }. Returns: 200 { success, message }.</remarks>
        [Authorize(Policy = "SuperAdmin")]
        [HttpPost("DenyUser")]
        public async Task<IActionResult> DenyUser([FromBody] StaffIdRequest request)
        {
            await _users.DenyAsync(request.Staff_ID);
            return Ok(new ApiMessage("User denied successfully"));
        }

        /// <summary>Removes (offboards) an active user: un-approves them, stamps RemovalDate, and revokes their sessions.</summary>
        /// <remarks>Expects (SuperAdmin): { staff_ID }. Returns: 200 { success, message }; 404 if the user does not exist.</remarks>
        [Authorize(Policy = "SuperAdmin")]
        [HttpPost("RemoveUser")]
        public async Task<IActionResult> RemoveUser([FromBody] StaffIdRequest request)
        {
            var rows = await _users.RemoveAsync(request.Staff_ID);
            if (rows == 0)
            {
                return NotFound(new ApiMessage("User not found.", false));
            }
            return Ok(new ApiMessage("User removed successfully"));
        }

        // ------------------------------------------------------------------
        // Device binding
        // ------------------------------------------------------------------

        /// <summary>Clears a user's device binding so they can register a new device on their next mobile login.</summary>
        /// <remarks>Expects (Admin): { staff_ID }. Returns: 200 { success, message }; 404 if the user does not exist.</remarks>
        [HttpPost("ResetDevice")]
        public async Task<IActionResult> ResetDevice([FromBody] StaffIdRequest request)
        {
            var rows = await _users.ResetDeviceAsync(request.Staff_ID);
            if (rows == 0)
            {
                return NotFound(new ApiMessage("User not found.", false));
            }
            return Ok(new ApiMessage("Device binding reset. The user can register a new device on next login."));
        }

        // ------------------------------------------------------------------
        // History
        // ------------------------------------------------------------------

        /// <summary>Lists approved users as approval-history records.</summary>
        /// <remarks>Expects (Admin): bearer token only. Returns: 200 with [{ id, staff_ID, name, approvalStatus, date }].</remarks>
        [HttpGet("ApprovalHistory")]
        public async Task<IActionResult> GetApprovalHistory()
        {
            return Ok(await _users.GetApprovalHistoryAsync());
        }

        /// <summary>Lists removed users as removal-history records.</summary>
        /// <remarks>Expects (Admin): bearer token only. Returns: 200 with [{ id, staff_ID, name, email, date }].</remarks>
        [HttpGet("DeletionHistory")]
        public async Task<IActionResult> GetRemovalHistory()
        {
            return Ok(await _users.GetRemovalHistoryAsync());
        }

        /// <summary>Deletes one history record (clears the user's approval/removal dates).</summary>
        /// <remarks>Expects (Admin): staff id in the URL. Returns: 200 { success, message }; 404 if no record matches.</remarks>
        [HttpDelete("DeletionHistory/{staffId}")]
        public async Task<IActionResult> DeleteHistoryRecord(string staffId)
        {
            var rows = await _users.ClearHistoryDatesAsync(staffId);
            if (rows == 0)
            {
                return NotFound(new ApiMessage("Record not found.", false));
            }
            return Ok(new ApiMessage("History record deleted."));
        }
    }
}
