using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Employee_History.Features.PasswordReset
{
    /// <summary>
    /// Email-based password reset (the unauthenticated "forgot password" flow):
    /// request a token by email, optionally verify it, then set a new password.
    /// Tokens are single-use and expire after 1 hour. All endpoints are
    /// anonymous and rate-limited (10 requests/minute per IP); responses never
    /// reveal whether an email address is registered.
    /// </summary>
    [Route("api/PasswordReset")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public class PasswordResetController : ApiControllerBase
    {
        private readonly IPasswordResetRepository _passwordReset;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(IPasswordResetRepository passwordReset, ILogger<PasswordResetController> logger)
        {
            _passwordReset = passwordReset;
            _logger = logger;
        }

        /// <summary>Emails a reset token to the address if it belongs to a user.</summary>
        /// <remarks>Expects: { email }. Returns: 200 { success, message } always (whether or not the email exists).</remarks>
        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            try
            {
                await _passwordReset.RequestResetAsync(request.Email);
            }
            catch (Exception ex)
            {
                // Never reveal to the caller whether the email exists or whether sending failed.
                _logger.LogError(ex, "Password reset request failed for {Email}", request.Email);
            }

            return Ok(new ApiMessage("If the email is registered, a password reset link has been sent."));
        }

        /// <summary>Checks a reset token before the new-password screen is shown.</summary>
        /// <remarks>Expects: { email, token }. Returns: 200 { success, message } when valid; 400 { message } when invalid or expired.</remarks>
        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyResetTokenRequest request)
        {
            var valid = await _passwordReset.IsTokenValidAsync(request.Email, request.Token);
            if (!valid)
            {
                return BadRequest(new ApiMessage("Invalid or expired token.", false));
            }
            return Ok(new ApiMessage("Token is valid."));
        }

        /// <summary>Sets a new password using a valid reset token, consumes the token, and revokes the user's sessions.</summary>
        /// <remarks>Expects: { email, token, newPassword (min 8 chars) }. Returns: 200 { success, message }; 400 { message } when the token is invalid or expired.</remarks>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetConfirmation request)
        {
            bool isResetSuccessful = await _passwordReset.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
            if (isResetSuccessful)
            {
                return Ok(new ApiMessage("Password has been reset successfully."));
            }
            return BadRequest(new ApiMessage("Invalid or expired token.", false));
        }
    }
}
