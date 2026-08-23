using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Users
{
    /// <summary>
    /// Self-service user endpoints — things any authenticated user (staff or
    /// admin) can do for their own account. Administrative user management
    /// lives in <see cref="UserAdminController"/>.
    /// </summary>
    [Route("api/User")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserRepository _users;

        public UsersController(IUserRepository users)
        {
            _users = users;
        }

        /// <summary>Returns the authenticated caller's own profile.</summary>
        /// <remarks>Expects: bearer token only. Returns: 200 with { staff_ID, name, email, phone_number, lab_role, approvalStatus, approvalDate }; 404 if the account no longer exists.</remarks>
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var staffId = CallerStaffId;
            if (staffId == null)
            {
                return Unauthorized(new ApiMessage("Invalid token.", false));
            }

            var user = await _users.GetByStaffIdAsync(staffId);
            if (user == null)
            {
                return NotFound(new ApiMessage("User not found.", false));
            }

            return Ok(new UserDto
            {
                Staff_ID = user.Staff_ID,
                Name = user.Name,
                Email = user.Email,
                Phone_number = user.Phone_number,
                Lab_role = user.Lab_role,
                ApprovalStatus = user.ApprovalStatus,
                ApprovalDate = user.ApprovalDate
            });
        }
    }
}
