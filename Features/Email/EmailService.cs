using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.ComponentModel.DataAnnotations;

namespace Employee_History.Features.Email
{
    /// <summary>An outbound email: <c>{ to, subject, body }</c>.</summary>
    public class EmailMessage
    {
        [Required, EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>SMTP email sending (settings: EmailHost, EmailUsername, EmailPassword).</summary>
    public interface IEmailService
    {
        void SendEmail(EmailMessage request);
        void SendPasswordResetEmail(string email, string resetLink);
        /// <summary>Sends an email and swallows (logs) failures. Use for non-critical mail on request paths.</summary>
        bool TrySendEmail(EmailMessage request);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private string RequiredSetting(string key) =>
            _config[key] ?? throw new InvalidOperationException($"Missing email setting '{key}'. Set the {key} environment variable.");

        public void SendEmail(EmailMessage request)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(RequiredSetting("EmailUsername")));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;
            email.Body = new TextPart(TextFormat.Plain) { Text = request.Body };
            Send(email);
        }

        public void SendPasswordResetEmail(string emailAddress, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(RequiredSetting("EmailUsername")));
            email.To.Add(MailboxAddress.Parse(emailAddress));
            email.Subject = "Password Reset Request";
            email.Body = new TextPart(TextFormat.Plain)
            {
                Text = $"Please reset your password using the following token: {resetLink}"
            };
            Send(email);
        }

        public bool TrySendEmail(EmailMessage request)
        {
            try
            {
                SendEmail(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", request.To);
                return false;
            }
        }

        private void Send(MimeMessage email)
        {
            using var client = new SmtpClient();
            client.Connect(RequiredSetting("EmailHost"), 587, SecureSocketOptions.StartTls);
            client.Authenticate(RequiredSetting("EmailUsername"), RequiredSetting("EmailPassword"));
            client.Send(email);
            client.Disconnect(true);
        }
    }
}
