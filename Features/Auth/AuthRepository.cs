using Dapper;
using Employee_History.Common.Security;
using Employee_History.Features.Users;
using Microsoft.Data.SqlClient;

namespace Employee_History.Features.Auth
{
    public class AuthRepository : IAuthRepository
    {
        private readonly SqlConnection _connection;

        public AuthRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> AuthenticateStaffAsync(string staffId)
        {
            return await _connection.QueryFirstOrDefaultAsync<User>(
                @"SELECT * FROM [User]
                  WHERE Staff_ID = @Staff_ID AND (Lab_role = 'B2' OR Lab_role = 'C3')",
                new { Staff_ID = staffId });
        }

        public async Task<User?> AuthenticateAdminAsync(string staffId, string password)
        {
            var user = await _connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM [User] WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });

            // Admin portal is for A1/B2 only.
            if (user == null || (user.Lab_role != "A1" && user.Lab_role != "B2"))
            {
                return null;
            }

            return PasswordHasher.VerifyPassword(password, user.Password) ? user : null;
        }

        public async Task<bool> IsApprovedAsync(string staffId)
        {
            var approvalStatus = await _connection.ExecuteScalarAsync<int?>(
                "SELECT ApprovalStatus FROM [User] WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });
            return approvalStatus == 1;
        }

        public async Task<int> ConfirmPasswordAsync(string staffId, string password)
        {
            var storedPassword = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Password FROM [User] WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });

            if (string.IsNullOrEmpty(storedPassword))
            {
                return -1;
            }
            return PasswordHasher.VerifyPassword(password, storedPassword) ? 0 : -1;
        }

        public async Task<bool> ChangePasswordAsync(string staffId, string currentPassword, string newPassword)
        {
            var storedPassword = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Password FROM [User] WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });

            if (string.IsNullOrEmpty(storedPassword) || !PasswordHasher.VerifyPassword(currentPassword, storedPassword))
            {
                return false;
            }

            await _connection.ExecuteAsync(
                "UPDATE [User] SET Password = @Password WHERE Staff_ID = @Staff_ID",
                new { Password = PasswordHasher.HashPassword(newPassword), Staff_ID = staffId });
            return true;
        }

        public async Task<int> StoreDeviceInfoAsync(string staffId, string deviceId, string deviceModel)
        {
            return await _connection.ExecuteAsync(
                "UPDATE [User] SET DeviceID = @DeviceID, DeviceModel = @DeviceModel WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId, DeviceID = deviceId, DeviceModel = deviceModel });
        }
    }
}
