using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IEmailService
    {
        void SendEmail(Email request);
        void SendPasswordResetEmail(string email, string resetLink);
    }
}
