using Dapper;
using Employee_History.Common.Models;
using Employee_History.Common.Security;
using Employee_History.Features.Email;
using Microsoft.Data.SqlClient;

namespace Employee_History.Features.Users
{
    public class UserRepository : IUserRepository
    {
        private const string PublicColumns =
            "Staff_ID, Name, Email, Phone_number, Lab_role, ApprovalStatus, ApprovalDate";

        private readonly SqlConnection _connection;
        private readonly IEmailService _emailService;
        private readonly IRefreshTokenStore _refreshTokens;

        public UserRepository(SqlConnection connection, IEmailService emailService, IRefreshTokenStore refreshTokens)
        {
            _connection = connection;
            _emailService = emailService;
            _refreshTokens = refreshTokens;
        }

        public async Task AddUserAsync(AddUserRequest request)
        {
            var existing = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Staff_ID FROM [User] WHERE Staff_ID = @Staff_ID", new { request.Staff_ID });
            if (existing != null)
            {
                throw new DuplicateStaffIdException(request.Staff_ID);
            }

            // Admin roles get a generated initial password; staff (C3) log in device-bound.
            string? password = null;
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", request.Staff_ID);
            parameters.Add("@Name", request.Name);
            parameters.Add("@Email", request.Email);
            parameters.Add("@Phone_number", request.Phone_number);
            parameters.Add("@Lab_role", request.Lab_role);

            string query;
            if (request.Lab_role == "A1" || request.Lab_role == "B2")
            {
                password = PasswordHasher.GenerateRandomPassword();
                parameters.Add("@Password", PasswordHasher.HashPassword(password));
                query = @"INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role, Password)
                          VALUES (@Staff_ID, @Name, @Email, @Phone_number, @Lab_role, @Password);";
            }
            else
            {
                query = @"INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role)
                          VALUES (@Staff_ID, @Name, @Email, @Phone_number, @Lab_role);";
            }

            await _connection.OpenAsync();
            using (var transaction = _connection.BeginTransaction())
            {
                await _connection.ExecuteAsync(query, parameters, transaction);

                await _connection.ExecuteAsync(
                    @"INSERT INTO [Notification] (Staff_ID, Message, RoleID, IsRead)
                      VALUES (@Staff_ID, @Message, 'A1', 0);",
                    new { request.Staff_ID, Message = $"{request.Staff_ID} waiting for approval" },
                    transaction);

                transaction.Commit();
            }

            // Email after commit; failure must not undo the add.
            if (password != null)
            {
                _emailService.TrySendEmail(new EmailMessage
                {
                    To = request.Email,
                    Subject = "Your new password",
                    Body = $"Your new password is: {password}. Please change it after your first login."
                });
            }
        }

        public async Task<User?> GetByStaffIdAsync(string staffId)
        {
            return await _connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM [User] WHERE Staff_ID = @Staff_ID", new { Staff_ID = staffId });
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return await _connection.QueryAsync<UserDto>(
                $"SELECT {PublicColumns} FROM [User] ORDER BY Name");
        }

        public async Task<PagedResult<UserDto>> GetPagedAsync(string? query, int page, int pageSize)
        {
            var sql = $@"
                SELECT COUNT(*) FROM [User]
                WHERE (@Query IS NULL OR Name LIKE '%' + @Query + '%' OR Staff_ID LIKE '%' + @Query + '%' OR Email LIKE '%' + @Query + '%');

                SELECT {PublicColumns}
                FROM [User]
                WHERE (@Query IS NULL OR Name LIKE '%' + @Query + '%' OR Staff_ID LIKE '%' + @Query + '%' OR Email LIKE '%' + @Query + '%')
                ORDER BY Name
                OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = await _connection.QueryMultipleAsync(sql, new { Query = query, Page = page, PageSize = pageSize });
            var total = await multi.ReadFirstAsync<int>();
            var data = await multi.ReadAsync<UserDto>();

            return new PagedResult<UserDto> { Data = data, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<IEnumerable<UserDto>> GetNonApprovedAsync()
        {
            return await _connection.QueryAsync<UserDto>(
                $@"SELECT {PublicColumns} FROM [User]
                   WHERE ApprovalStatus = 0 AND RemovalDate IS NULL");
        }

        public async Task<IEnumerable<UserDto>> GetByRoleAsync(string labRole)
        {
            return await _connection.QueryAsync<UserDto>(
                $"SELECT {PublicColumns} FROM [User] WHERE Lab_role = @Lab_role",
                new { Lab_role = labRole });
        }

        public async Task<int> ApproveAsync(string staffId)
        {
            string sql = @"
                BEGIN TRANSACTION;
                BEGIN TRY
                    UPDATE [User]
                    SET ApprovalStatus = 1,
                        ApprovalDate = GETUTCDATE(),
                        RemovalDate = NULL
                    WHERE Staff_ID = @Staff_ID;

                    UPDATE [Notification]
                    SET IsRead = 1
                    WHERE Staff_ID = @Staff_ID AND RoleID = 'A1';

                    INSERT INTO [Notification] (Staff_ID, IsRead, RoleID, Message)
                    VALUES (@Staff_ID, 0, 'B2', 'The staff with StaffID ' + CAST(@Staff_ID AS NVARCHAR(50)) + ' has been approved');

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return await _connection.ExecuteAsync(sql, new { Staff_ID = staffId });
        }

        public async Task<int> DenyAsync(string staffId)
        {
            string sql = @"
                BEGIN TRANSACTION;
                BEGIN TRY
                    UPDATE [Notification]
                    SET IsRead = 1
                    WHERE Staff_ID = @Staff_ID AND RoleID = 'A1';

                    INSERT INTO [Notification] (Staff_ID, IsRead, RoleID, Message)
                    VALUES (@Staff_ID, 0, 'B2', 'The staff with StaffID ' + CAST(@Staff_ID AS NVARCHAR(50)) + ' has been denied');

                    DELETE FROM [User]
                    WHERE Staff_ID = @Staff_ID;

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return await _connection.ExecuteAsync(sql, new { Staff_ID = staffId });
        }

        public async Task<int> RemoveAsync(string staffId)
        {
            var rows = await _connection.ExecuteAsync(
                @"UPDATE [User] SET ApprovalStatus = 0, RemovalDate = GETUTCDATE() WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });

            if (rows > 0)
            {
                await _refreshTokens.RevokeAllForUserAsync(staffId);
            }
            return rows;
        }

        public async Task<IEnumerable<ApprovalRecordDto>> GetApprovalHistoryAsync()
        {
            return await _connection.QueryAsync<ApprovalRecordDto>(
                @"SELECT Staff_ID AS Id, Staff_ID, Name, ApprovalStatus, ApprovalDate AS Date
                  FROM [User]
                  WHERE ApprovalStatus = 1 AND ApprovalDate IS NOT NULL
                  ORDER BY ApprovalDate DESC");
        }

        public async Task<IEnumerable<RemovalRecordDto>> GetRemovalHistoryAsync()
        {
            return await _connection.QueryAsync<RemovalRecordDto>(
                @"SELECT Staff_ID AS Id, Staff_ID, Name, Email, RemovalDate AS Date
                  FROM [User]
                  WHERE RemovalDate IS NOT NULL
                  ORDER BY RemovalDate DESC");
        }

        public async Task<int> ClearHistoryDatesAsync(string staffId)
        {
            return await _connection.ExecuteAsync(
                "UPDATE [User] SET ApprovalDate = NULL, RemovalDate = NULL WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });
        }

        public async Task<int> ResetDeviceAsync(string staffId)
        {
            return await _connection.ExecuteAsync(
                "UPDATE [User] SET DeviceID = NULL, DeviceModel = NULL WHERE Staff_ID = @Staff_ID",
                new { Staff_ID = staffId });
        }
    }
}
