using Dapper;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
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
            // Hash the password
          

            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Name", Name);
            parameters.Add("@Email", Email);
            parameters.Add("@Phone_number", Phone_number);
            parameters.Add("@Lab_role", Lab_role);


            await _connection.OpenAsync();

            var query = @"IF EXISTS (SELECT 1 FROM [User] WHERE Staff_ID = @Staff_ID)
                  BEGIN
                    THROW 51000, 'Staff ID already exists.', 1;
                  END;
                  INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role, Password) 
                  VALUES (@Staff_ID, @Name, @Email, @Phone_number, @Lab_role, @Password);
                  SELECT * FROM [User] WHERE Staff_ID = @Staff_ID;";

            var result = await _connection.QueryFirstOrDefaultAsync<User>(query, parameters);

            return result;
        }
        private string HashPassword(string password)
        {
            // Generate a salt
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Format the hashed password with the salt
            return $"$bcrypt$v=2$rounds=10${Convert.ToBase64String(salt)}${hashed}";
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
            string hashedPassword = HashPassword(Password);
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Password", hashedPassword);

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

        

        public async Task<User> AuthenticateAsync(string staff_ID, string generatedCode)
        {
            // SQL query to check if Staff_ID and generatedCode exist in the database
            var sql = @"
            SELECT *
            FROM [User]
            WHERE Staff_ID = @Staff_ID AND Code = @GeneratedCode";

            // Execute the query and retrieve the user information
            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Staff_ID = staff_ID, GeneratedCode = generatedCode });

            // Return the authenticated user
            return user;
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
        public async Task<IEnumerable<User>> GetApprovalDataAsync(int daysBehind)
        {

            string query = "SELECT * FROM [User] WHERE ApprovalDate >= DATEADD(day, -@DaysBehind, GETDATE())";
            return await _connection.QueryAsync<User>(query, new { DaysBehind = daysBehind });

        }

        public async Task<IEnumerable<User>> GetRemovalDataAsync(int daysBehind)
        {

            string query = "SELECT * FROM [User] WHERE RemovalDate >= DATEADD(day, -@DaysBehind, GETDATE())";
            return await _connection.QueryAsync<User>(query, new { DaysBehind = daysBehind });

        }
        public async Task<IEnumerable<User>> GetEmployeesByRoleIDAsync(string Lab_role)
        {
           
                string query = "SELECT * FROM [User] WHERE Lab_role = @Lab_role";
                return await _connection.QueryAsync<User>(query, new { Lab_role = Lab_role });
            
        }

        public async Task<string> InsertGeneratedCode(string Staff_ID, HttpContext httpContext)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string ipAddress = httpContext.Connection.RemoteIpAddress.ToString();

                var random = new Random();
                var randomChars = "A$*$&#(B3324533CDEFGHIJK--=#%$7$LMNOP#%^&TUV$$W#X@YZ#0123456789";
                var codeBuilder = new StringBuilder();

                // Add staff ID to the code
                codeBuilder.Append(random.Next(0, 100) + Staff_ID);

                // Add random characters to the code
                for (int i = 0; i < 4; i++)
                {
                    codeBuilder.Append(randomChars[random.Next(0, randomChars.Length)]);
                }

                var generatedCodeValue = codeBuilder.ToString();

                var insertedCode = await connection.QuerySingleAsync<string>(
                    @"UPDATE [User]
                      SET Code = COALESCE(@Code, Code),
                          UserIPAddress = COALESCE(@UserIPAddress, UserIPAddress)
                      WHERE Staff_ID = @Staff_ID;
                      SELECT Code FROM [User] WHERE Staff_ID = @Staff_ID;",
                    new { Code = generatedCodeValue, Staff_ID, UserIPAddress = ipAddress });


                return insertedCode; // Return the generated code as the response
            }
        }



    }
}
