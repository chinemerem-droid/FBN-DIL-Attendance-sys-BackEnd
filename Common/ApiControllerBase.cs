using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_History.Common
{
    /// <summary>
    /// Base class for all API controllers. Exposes the caller's identity
    /// (staff id and role) taken from the validated JWT.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>Staff ID of the authenticated caller (JWT "nameid" claim).</summary>
        protected string? CallerStaffId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>Role of the authenticated caller (JWT "LabRole" claim): A1, B2 or C3.</summary>
        protected string? CallerRole => User.FindFirstValue("LabRole");

        /// <summary>True when the caller is a super admin (A1) or sub admin (B2).</summary>
        protected bool CallerIsAdmin => CallerRole == "A1" || CallerRole == "B2";
    }
}
