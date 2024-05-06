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
            return await _connection.QueryFirstOrDefaultAsync<User>(@"RemoveUSer", parameters, commandType: System.Data.CommandType.StoredProcedure);
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
    }
}
