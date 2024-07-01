using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Employee_History.DappaRepo;
using Microsoft.AspNetCore.Authorization;

namespace Employee_History.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IDapperUser _dapperUser;
        private readonly IEmailService _emailService;

        public PasswordResetController(IDapperUser dapperUser, IEmailService emailService)
        {
            _dapperUser = dapperUser;
            _emailService = emailService;
        }
      
        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            await _dapperUser.RequestPasswordResetAsync(request.Email);
            return Ok("Password reset link has been sent to your email.");
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetConfirmation request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("Token and new password are required.");
            }

            bool isResetSuccessful = await _dapperUser.VerifyPasswordResetTokenAsync(request.email,request.Token, request.NewPassword);
            if (isResetSuccessful)
            {
                return Ok("Password has been reset successfully.");
            }
            else
            {
                return BadRequest("Invalid or expired token.");
            }
        }
    }
}




