namespace Employee_History.Models
{
    public class PasswordResetConfirmation
    {
        public string email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
