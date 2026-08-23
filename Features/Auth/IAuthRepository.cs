using Employee_History.Features.Users;

namespace Employee_History.Features.Auth
{
    /// <summary>Credential and device-binding checks for logins and password changes.</summary>
    public interface IAuthRepository
    {
        /// <summary>Returns the user when the staff id exists with role B2/C3 (mobile login), else null.</summary>
        Task<User?> AuthenticateStaffAsync(string staffId);
        /// <summary>Returns the user when the staff id + password match an A1/B2 account, else null.</summary>
        Task<User?> AuthenticateAdminAsync(string staffId, string password);
        Task<bool> IsApprovedAsync(string staffId);
        /// <summary>0 when the password matches, -1 when it does not (or the user has none).</summary>
        Task<int> ConfirmPasswordAsync(string staffId, string password);
        /// <summary>Verifies the current password and stores a new hash. False when the current password is wrong.</summary>
        Task<bool> ChangePasswordAsync(string staffId, string currentPassword, string newPassword);
        /// <summary>Binds a device to a staff account (first mobile login).</summary>
        Task<int> StoreDeviceInfoAsync(string staffId, string deviceId, string deviceModel);
    }
}
