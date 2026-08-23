using System.ComponentModel.DataAnnotations;

namespace Employee_History.Features.PasswordReset
{
    /// <summary>Body for requesting a reset token by email.</summary>
    public class PasswordResetRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Body for validating a reset token before showing the new-password screen.</summary>
    public class VerifyResetTokenRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>Body for completing a password reset.</summary>
    public class PasswordResetConfirmation
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
