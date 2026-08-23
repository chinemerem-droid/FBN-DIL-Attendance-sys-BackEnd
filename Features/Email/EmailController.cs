using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Email
{
    /// <summary>
    /// Manual email sending from the organization's account. Super-admin (A1)
    /// only — this endpoint was previously an unauthenticated open relay.
    /// </summary>
    [Route("api/Email")]
    [Authorize(Policy = "SuperAdmin")]
    public class EmailController : ApiControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        /// <summary>Sends an email from the configured organization account.</summary>
        /// <remarks>Expects (SuperAdmin): { to, subject, body }. Returns: 200 { success, message }; 400 on invalid input.</remarks>
        [HttpPost]
        public IActionResult SendEmail([FromBody] EmailMessage request)
        {
            _emailService.SendEmail(request);
            return Ok(new ApiMessage("Email sent."));
        }
    }
}
