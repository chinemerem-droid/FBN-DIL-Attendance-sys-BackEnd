using Dapper;
using Employee_History.Common.Security;
using Employee_History.Features.Email;
using Employee_History.Features.Users;
using Microsoft.Data.SqlClient;

namespace Employee_History.Features.PasswordReset
{
    /// <summary>Data access for the email-based password reset flow.</summary>
    public interface IPasswordResetRepository
    {
        /// <summary>Issues a single-use, 1-hour token and emails a reset link. Silently does nothing for unknown emails.</summary>
        Task RequestResetAsync(string email);
        Task<bool> IsTokenValidAsync(string email, string token);
        /// <summary>Sets the new password, consumes the token, and revokes the user's sessions. False on an invalid/expired token.</summary>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }

    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly SqlConnection _connection;
        private readonly IEmailService _emailService;
        private readonly IRefreshTokenStore _refreshTokens;
        private readonly IConfiguration _configuration;

        public PasswordResetRepository(
            SqlConnection connection,
            IEmailService emailService,
            IRefreshTokenStore refreshTokens,
            IConfiguration configuration)
        {
            _connection = connection;
            _emailService = emailService;
            _refreshTokens = refreshTokens;
            _configuration = configuration;
        }

        public async Task RequestResetAsync(string email)
        {
            var user = await _connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM [User] WHERE Email = @Email", new { Email = email });

            if (user == null)
            {
                return; // Do not reveal whether the email exists.
            }

            string token = TokenGenerator.GenerateSecureToken();

            await _connection.ExecuteAsync(
                "DELETE FROM PasswordResetTokens WHERE Staff_ID = @Staff_ID",
                new { user.Staff_ID });
            await _connection.ExecuteAsync(
                @"INSERT INTO PasswordResetTokens (Staff_ID, Token, ExpiryDate)
                  VALUES (@Staff_ID, @Token, DATEADD(hour, 1, GETUTCDATE()))",
                new { user.Staff_ID, Token = token });

            string resetLink = $"{_configuration["AppSettings:PasswordResetUrl"]}?token={token}";
            _emailService.SendPasswordResetEmail(email, resetLink);
        }

        public async Task<bool> IsTokenValidAsync(string email, string token)
        {
            return await GetStaffIdForValidTokenAsync(email, token) != null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var staffId = await GetStaffIdForValidTokenAsync(email, token);
            if (staffId == null)
            {
                return false;
            }

            await _connection.ExecuteAsync(
                "UPDATE [User] SET Password = @Password WHERE Staff_ID = @Staff_ID",
                new { Password = PasswordHasher.HashPassword(newPassword), Staff_ID = staffId });
            await _connection.ExecuteAsync(
                "DELETE FROM PasswordResetTokens WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });
            await _refreshTokens.RevokeAllForUserAsync(staffId);

            return true;
        }

        private async Task<string?> GetStaffIdForValidTokenAsync(string email, string token)
        {
            return await _connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT t.Staff_ID
                  FROM PasswordResetTokens t
                  INNER JOIN [User] u ON u.Staff_ID = t.Staff_ID
                  WHERE t.Token = @Token AND u.Email = @Email AND t.ExpiryDate > GETUTCDATE()",
                new { Token = token, Email = email });
        }
    }
}
