using Employee_History.Common;
using Employee_History.Common.Models;
using Employee_History.Common.Security;
using Employee_History.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Employee_History.Features.Auth
{
    /// <summary>
    /// Authentication and session management: admin portal login, mobile
    /// (device-bound) staff login, token refresh/rotation, logout, and
    /// password confirm/change. Login and refresh are rate-limited
    /// (10 requests/minute per IP) and are the only anonymous endpoints here.
    /// Legacy routes under /api/User/* are preserved for existing clients.
    /// </summary>
    [Route("api/Auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthRepository _auth;
        private readonly IUserRepository _users;
        private readonly IRefreshTokenStore _refreshTokens;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthRepository auth,
            IUserRepository users,
            IRefreshTokenStore refreshTokens,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _auth = auth;
            _users = users;
            _refreshTokens = refreshTokens;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>Logs an admin (A1/B2) into the web portal.</summary>
        /// <remarks>Expects: { staff_ID, password }. Returns: 200 { message, token, refreshToken } — the JWT carries nameid, unique_name, LabRole, exp; 401 { message } on bad credentials or a non-admin role.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("/api/User/loginAdmin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminRequest request)
        {
            var user = await _auth.AuthenticateAdminAsync(request.Staff_ID, request.Password);
            if (user == null)
            {
                return Unauthorized(new ApiMessage("Invalid Staff ID, Password, or insufficient role.", false));
            }

            var token = TokenGenerator.GenerateAccessToken(user, _configuration);
            var refreshToken = await IssueRefreshTokenAsync(user.Staff_ID);

            return Ok(new LoginResponse { Message = "Login successful", Token = token, RefreshToken = refreshToken });
        }

        /// <summary>Logs a staff member (B2/C3) into the mobile app. The first login binds the device; later logins must come from the same device until an admin resets it.</summary>
        /// <remarks>Expects: { staff_ID, deviceID, deviceModel }. Returns: 200 { message, token, refreshToken }; 401 { message } for an unknown staff id, unapproved account, or unrecognised device.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("/api/User/loginuser")]
        public async Task<IActionResult> LoginUser([FromBody] LoginUserRequest request)
        {
            var user = await _auth.AuthenticateStaffAsync(request.Staff_ID);
            if (user == null)
            {
                return Unauthorized(new ApiMessage("Invalid Staff ID.", false));
            }

            if (!await _auth.IsApprovedAsync(request.Staff_ID))
            {
                return Unauthorized(new ApiMessage("User is not approved.", false));
            }

            // Device binding: bind on first login; afterwards the same device is required.
            if (string.IsNullOrEmpty(user.DeviceID))
            {
                await _auth.StoreDeviceInfoAsync(request.Staff_ID, request.DeviceID, request.DeviceModel);
            }
            else if (user.DeviceID != request.DeviceID || user.DeviceModel != request.DeviceModel)
            {
                return Unauthorized(new ApiMessage("This device is not registered to this account. Contact an administrator to reset your device.", false));
            }

            var token = TokenGenerator.GenerateAccessToken(user, _configuration);
            var refreshToken = await IssueRefreshTokenAsync(user.Staff_ID);

            return Ok(new LoginResponse { Message = "Login successful.", Token = token, RefreshToken = refreshToken });
        }

        /// <summary>Exchanges a valid refresh token for a new access + refresh token pair (the used token is revoked).</summary>
        /// <remarks>Expects: { refreshToken }. Returns: 200 { message, token, refreshToken }; 401 { message } when the token is invalid, expired, revoked, or the user is no longer active.</remarks>
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new ApiMessage("Refresh token is required.", false));
            }

            var staffId = await _refreshTokens.GetStaffIdAsync(request.RefreshToken);
            if (staffId == null)
            {
                return Unauthorized(new ApiMessage("Invalid or expired refresh token.", false));
            }

            var user = await _users.GetByStaffIdAsync(staffId);
            if (user == null || !user.ApprovalStatus)
            {
                return Unauthorized(new ApiMessage("User is no longer active.", false));
            }

            // Rotate: revoke the used token, issue a new pair.
            await _refreshTokens.RevokeAsync(request.RefreshToken);
            var newRefreshToken = await IssueRefreshTokenAsync(staffId);
            var accessToken = TokenGenerator.GenerateAccessToken(user, _configuration);

            return Ok(new LoginResponse { Message = "Token refreshed.", Token = accessToken, RefreshToken = newRefreshToken });
        }

        /// <summary>Logs the caller out by revoking their refresh token(s). Pass a specific token to revoke only that session; omit it to revoke all of the caller's sessions.</summary>
        /// <remarks>Expects: bearer token; optional { refreshToken }. Returns: 200 { success, message }.</remarks>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest? request = null)
        {
            if (!string.IsNullOrEmpty(request?.RefreshToken))
            {
                await _refreshTokens.RevokeAsync(request.RefreshToken);
            }
            else if (CallerStaffId != null)
            {
                await _refreshTokens.RevokeAllForUserAsync(CallerStaffId);
            }
            return Ok(new ApiMessage("Logged out."));
        }

        /// <summary>Re-confirms a password (used by the admin portal before sensitive actions like adding users).</summary>
        /// <remarks>Expects: bearer token + { staff_ID, password }. Returns: 200 { success, message }; 401 { message } when the password is wrong or the user has none.</remarks>
        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost("/api/User/ConfirmPassword")]
        public async Task<IActionResult> ConfirmPassword([FromBody] ConfirmPasswordRequest request)
        {
            int result = await _auth.ConfirmPasswordAsync(request.Staff_ID, request.Password);
            if (result == 0)
            {
                return Ok(new ApiMessage("Password confirmed successfully."));
            }
            return Unauthorized(new ApiMessage("Incorrect password or user not found.", false));
        }

        /// <summary>Changes the caller's own password and revokes all of their sessions.</summary>
        /// <remarks>Expects: bearer token + { currentPassword, newPassword (min 8 chars) }. Returns: 200 { success, message }; 401 { message } when the current password is wrong.</remarks>
        [Authorize]
        [HttpPost("/api/User/ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var staffId = CallerStaffId;
            if (staffId == null)
            {
                return Unauthorized(new ApiMessage("Invalid token.", false));
            }

            var changed = await _auth.ChangePasswordAsync(staffId, request.CurrentPassword, request.NewPassword);
            if (!changed)
            {
                return Unauthorized(new ApiMessage("Current password is incorrect.", false));
            }

            await _refreshTokens.RevokeAllForUserAsync(staffId);
            return Ok(new ApiMessage("Password changed successfully."));
        }

        private async Task<string> IssueRefreshTokenAsync(string staffId)
        {
            var token = TokenGenerator.GenerateSecureToken();
            var days = _configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;

            try
            {
                await _refreshTokens.StoreAsync(staffId, token, DateTime.UtcNow.AddDays(days));
            }
            catch (Exception ex)
            {
                // Refresh tokens require the RefreshTokens table (migration 001).
                // Login still succeeds without one.
                _logger.LogWarning(ex, "Could not store refresh token for {StaffId}. Has migration 001 been run?", staffId);
                return string.Empty;
            }
            return token;
        }
    }
}
