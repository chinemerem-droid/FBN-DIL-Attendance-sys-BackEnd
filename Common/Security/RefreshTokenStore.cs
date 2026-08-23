using Dapper;
using Microsoft.Data.SqlClient;

namespace Employee_History.Common.Security
{
    /// <summary>Persists and validates rotating refresh tokens (RefreshTokens table).</summary>
    public interface IRefreshTokenStore
    {
        Task StoreAsync(string staffId, string token, DateTime expiryUtc);
        /// <summary>Returns the staff id for a valid (unrevoked, unexpired) token, else null.</summary>
        Task<string?> GetStaffIdAsync(string token);
        Task RevokeAsync(string token);
        Task RevokeAllForUserAsync(string staffId);
    }

    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly SqlConnection _connection;

        public RefreshTokenStore(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task StoreAsync(string staffId, string token, DateTime expiryUtc)
        {
            await _connection.ExecuteAsync(
                @"INSERT INTO RefreshTokens (Staff_ID, Token, ExpiryDate, Revoked)
                  VALUES (@Staff_ID, @Token, @ExpiryDate, 0)",
                new { Staff_ID = staffId, Token = token, ExpiryDate = expiryUtc });
        }

        public async Task<string?> GetStaffIdAsync(string token)
        {
            return await _connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT Staff_ID FROM RefreshTokens
                  WHERE Token = @Token AND Revoked = 0 AND ExpiryDate > GETUTCDATE()",
                new { Token = token });
        }

        public async Task RevokeAsync(string token)
        {
            await _connection.ExecuteAsync(
                "UPDATE RefreshTokens SET Revoked = 1 WHERE Token = @Token",
                new { Token = token });
        }

        public async Task RevokeAllForUserAsync(string staffId)
        {
            await _connection.ExecuteAsync(
                "UPDATE RefreshTokens SET Revoked = 1 WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });
        }
    }
}
