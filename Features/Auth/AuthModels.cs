using System.ComponentModel.DataAnnotations;

namespace Employee_History.Features.Auth
{
    /// <summary>Body for the admin portal login.</summary>
    public class LoginAdminRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Body for the mobile (staff) login. The first login binds the device.</summary>
    public class LoginUserRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        [Required]
        public string DeviceID { get; set; } = string.Empty;

        [Required]
        public string DeviceModel { get; set; } = string.Empty;
    }

    /// <summary>Body for re-confirming the caller's password (used by sensitive admin flows).</summary>
    public class ConfirmPasswordRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Body for an authenticated self-service password change.</summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Body for exchanging a refresh token for a new token pair, or for revoking one on logout.</summary>
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>Login/refresh response: <c>{ message, token, refreshToken }</c>.</summary>
    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
