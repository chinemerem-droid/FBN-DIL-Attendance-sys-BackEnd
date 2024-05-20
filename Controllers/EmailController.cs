using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MimeKit.Text;
using MailKit.Security;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using MailKit.Net.Smtp;
using Employee_History.Interface;
using Employee_History.Models;

namespace Mailkit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }
        [HttpPost]
        public IActionResult SendEmail(Email request)
        {
             _emailService.SendEmail(request);
            return Ok();
        }
    }
}
