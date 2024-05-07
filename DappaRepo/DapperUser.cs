using Dapper;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Employee_History.DappaRepo
{
    public class DapperUser : IDapperUser
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;
        public DapperUser(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<User> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role)
        {
            var parameters = new DynamicParameters();
            parameters.Add(@"Staff_ID", Staff_ID);
            parameters.Add(@"Name", Name);
            parameters.Add(@"Email", Email);
            parameters.Add(@"Phone_number", Phone_number);
            parameters.Add(@"Lab_role", Lab_role);
            return await _connection.QueryFirstOrDefaultAsync<User>(@"AddUSer", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<User> RemoveUser(string Staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add(@"Staff_ID", Staff_ID);
            return await _connection.QueryFirstOrDefaultAsync<User>(@"RemoveUser", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _connection.QueryAsync<User>("AllUsers", commandType: System.Data.CommandType.StoredProcedure);
        }
        public async Task<User> AddPassword(string Staff_ID, string Password)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Password", Password);

            // Assuming _connection is your SqlConnection object
            return await _connection.QueryFirstOrDefaultAsync<User>("AddPassword", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ConfirmPassword(string Staff_ID, string Password)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Password", Password);
            return await _connection.ExecuteScalarAsync<int>("ConfirmPassword", parameters, commandType: CommandType.StoredProcedure);
        }


        public async Task<bool> IsUserApprovedAsync(string staff_ID)
        {
            var sql = "SELECT ApprovalStatus FROM [User] WHERE Staff_ID = @Staff_ID";
            var approvalStatus = await _connection.ExecuteScalarAsync<int?>(sql, new { Staff_ID = staff_ID });

            if (approvalStatus.HasValue)
            {
                return approvalStatus.Value == 1;
            }
            else
            {
                // Handle the case where approvalStatus is null (not approved)
                return false;
            }
        }



        public async Task<User> AuthenticateAsync(string staff_ID, string password)
        {
            var sql = "SELECT * FROM [User] WHERE Staff_ID = @Staff_ID AND Password = @Password";
            return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Staff_ID = staff_ID, Password = password });
        }

        public async Task<IEnumerable<User>> GetNonApprovedAsync()
        {
            var sql = "SELECT * FROM [User] WHERE ApprovalStatus = 0";
            var nonApprovedUsers = await _connection.QueryAsync<User>(sql);
            return nonApprovedUsers;
        }

        public async Task<int> ApproveUserAsync(string staff_ID)
        {
            var sql = "UPDATE [User] SET ApprovalStatus = 1 WHERE Staff_ID = @Staff_ID";
            return await _connection.ExecuteAsync(sql, new { Staff_ID = staff_ID});
        }


    }
}
