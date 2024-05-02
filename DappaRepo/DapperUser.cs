using Dapper;
using Employee_History.Models;
using Microsoft.Data.SqlClient;
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

        public async Task<User> AddUser(string StaffID, string Name, string Email, string Device, long Phone_number, string Lab_role, string Password)
        {
            var parameters = new DynamicParameters();
            parameters.Add(@"StaffID", StaffID);
            parameters.Add(@"Name", Name);
            parameters.Add(@"Email", Email);
            parameters.Add(@"Device", Device);
            parameters.Add(@"Phone_number", Phone_number);
            parameters.Add(@"Lab_role", Lab_role);
            parameters.Add(@"Password", Password);
            return await _connection.QueryFirstOrDefaultAsync<User>(@"AddUSer", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<User> RemoveUser(string StaffID)
        {
            var parameters = new DynamicParameters();
            parameters.Add(@"StaffID", StaffID);
            return await _connection.QueryFirstOrDefaultAsync<User>(@"RemoveUSer", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _connection.QueryAsync<User>("AllUsers", commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
