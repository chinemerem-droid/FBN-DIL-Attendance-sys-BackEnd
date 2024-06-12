using Dapper;
using Employee_History.Interface;
using Employee_History.Models;
using MailKit.Security;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MimeKit.Text;
using MimeKit;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Employee_History.Interface;
using MailKit.Security;
using MimeKit.Text;
using MimeKit;
using Employee_History.Models;
using MailKit.Net.Smtp;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Employee_History.DappaRepo
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration confige;

        public EmailService(IConfiguration config)
        {
            confige = config;
        }
        public void SendEmail(Email request)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(confige.GetSection("EmailUsername").Value));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;
            email.Body = new TextPart(TextFormat.Plain) { Text = request.Body };

            using (var client = new SmtpClient())
            {
                client.Connect(confige.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
                client.Authenticate(confige.GetSection("EmailUsername").Value, confige.GetSection("EmailPassword").Value);
                client.Send(email);
                client.Disconnect(true);
            }
        }
        public void SendPasswordResetEmail(string email, string resetLink)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(MailboxAddress.Parse(confige.GetSection("EmailUsername").Value));
            emailMessage.To.Add(MailboxAddress.Parse(email));
            emailMessage.Subject = "Password Reset Request";
            emailMessage.Body = new TextPart(TextFormat.Plain)
            {
                Text = $"Please reset your password using the following token: {resetLink}"
            };

            using (var client = new SmtpClient())
            {
                client.Connect(confige.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
                client.Authenticate(confige.GetSection("EmailUsername").Value, confige.GetSection("EmailPassword").Value);
                client.Send(emailMessage);
                client.Disconnect(true);
            }
        }
    }
    public class DapperUser : IDapperUser
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;
        private readonly IEmailService _emailService;
        public DapperUser(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            _emailService = emailService;
        }

        public async Task<IEnumerable<Notification>> GetNotification()
        {
            var sql = "SELECT * FROM [Notification]";
            var notification = await _connection.QueryAsync<Notification>(sql);
            return notification;
        }


        public async Task<bool> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Name", Name);
            parameters.Add("@Email", Email);
            parameters.Add("@Phone_number", Phone_number);
            parameters.Add("@Lab_role", Lab_role);

            string query;
            string password = null;

            if (Lab_role == "A1" || Lab_role == "B2")
            {
                // Generate a random password
                password = GenerateRandomPassword(Staff_ID);

                // Hash the password
                string hashedPassword = HashPassword(password);
                parameters.Add("@Password", hashedPassword);

                query = @"
        IF EXISTS (SELECT 1 FROM [User] WHERE Staff_ID = @Staff_ID)
        BEGIN
            THROW 51000, 'Staff ID already exists.', 1;
        END
        INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role, Password) 
        VALUES (@Staff_ID, @Name, @Email, @Phone_number, @Lab_role, @Password);
        SELECT * FROM [User] WHERE Staff_ID = @Staff_ID;";
            }
            else if (Lab_role == "C3")
            {
                query = @"
        IF EXISTS (SELECT 1 FROM [User] WHERE Staff_ID = @Staff_ID)
        BEGIN
            THROW 51000, 'Staff ID already exists.', 1;
        END
        INSERT INTO [User] (Staff_ID, Name, Email, Phone_number, Lab_role) 
        VALUES (@Staff_ID, @Name, @Email, @Phone_number, @Lab_role);
        SELECT * FROM [User] WHERE Staff_ID = @Staff_ID;";
            }
            else
            {
                throw new ArgumentException("Invalid Lab_role specified.");
            }

          
                try
                {
                    await _connection.OpenAsync();

                    using (var transaction = _connection.BeginTransaction())
                    {
                        var user = await _connection.QueryFirstOrDefaultAsync<User>(
                            query, parameters, transaction);

                        if (user != null)
                        {
                            var notificationParameters = new DynamicParameters();
                            notificationParameters.Add("@Staff_ID", Staff_ID);
                            notificationParameters.Add("@Message", $"{Staff_ID} waiting for approval");

                            string notificationQuery = @"
                        INSERT INTO [Notification] (Staff_ID, Message) 
                        VALUES (@Staff_ID, @Message);";

                            await _connection.ExecuteAsync(notificationQuery, notificationParameters, transaction);

                            transaction.Commit();

                            if (password != null)
                            {
                                var emailRequest = new Email
                                {
                                    To = Email,
                                    Subject = "Your new password",
                                    Body = $"Your new password is: {password}"
                                };
                                _emailService.SendEmail(emailRequest);
                            }

                            return true;
                        }
                        else
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }
            
        }


        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM [User] WHERE Email = @Email", new { Email = email });

            if (user != null)
            {
                string token = GeneratePasswordResetToken(user.Staff_ID);
                string resetLink = $"{_configuration.GetSection("AppSettings:PasswordResetUrl").Value}?token={token}";

                // Save the token to the database or an in-memory store
                await _connection.ExecuteAsync(
                    "INSERT INTO PasswordResetTokens (Staff_ID, Token, ExpiryDate) VALUES (@Staff_ID, @Token, DATEADD(hour, 1, GETUTCDATE()))",
                    new { Staff_ID = user.Staff_ID, Token = token });

                _emailService.SendPasswordResetEmail(email, resetLink);
            }
        }
        private string GeneratePasswordResetToken(string Staff_ID)
        {
            using (var hmac = new HMACSHA256())
            {
                var tokenBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(Staff_ID + DateTime.UtcNow.ToString()));
                // Convert the token bytes to Base64 string and take the first 5 characters
                var tokenBase64 = Convert.ToBase64String(tokenBytes);
                return tokenBase64.Substring(0, 5);
            }
        }


        public async Task<bool> VerifyPasswordResetTokenAsync(string token, string newPassword)
        {
            var result = await _connection.QueryFirstOrDefaultAsync(
                "SELECT Staff_ID FROM PasswordResetTokens WHERE Token = @Token AND ExpiryDate > GETUTCDATE()",
                new { Token = token });

            if (result != null)
            {
                var hashedPassword = HashPassword(newPassword);
                await _connection.ExecuteAsync(
                    "UPDATE [User] SET Password = @Password WHERE Staff_ID = @Staff_ID",
                    new { Password = hashedPassword, Staff_ID = result.Staff_ID });

                await _connection.ExecuteAsync(
                    "DELETE FROM PasswordResetTokens WHERE Token = @Token",
                    new { Token = token });

                return true;
            }

            return false;
        }
        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"$bcrypt$v=2$rounds=10${Convert.ToBase64String(salt)}${hashed}";
        }

        private string GenerateRandomPassword(string Staff_ID)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] staffIdBytes = Encoding.UTF8.GetBytes(Staff_ID);
                byte[] hashBytes = sha256.ComputeHash(staffIdBytes);
                StringBuilder passwordBuilder = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    passwordBuilder.Append(hashBytes[i].ToString("X2")); // Convert each byte to a hex string
                }

                // Return the first 12 characters to make it a manageable password length
                return passwordBuilder.ToString().Substring(0, 12);
            }
        }

        public async Task<int> RemoveUser(string Staff_ID)
        {
            string sql = @"
              UPDATE[User]
            SET ApprovalStatus = 0,
                RemovalDate = GETUTCDATE()
            WHERE Staff_ID = @Staff_ID";
            return await _connection.ExecuteAsync(sql, new { Staff_ID = Staff_ID });


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

        public async Task<int> ConfirmPassword(string staff_ID, string password)
        {
            const string query = @"
            SELECT Password
            FROM [User]
            WHERE Staff_ID = @Staff_ID";

            var storedPassword = await _connection.QueryFirstOrDefaultAsync<string>(query, new { Staff_ID = staff_ID });

            if (storedPassword == null)
            {
                // User not found
                return -1;
            }

            if (VerifyPassword(password, storedPassword))
            {
                // Password matches
                return 0;
            }
            else
            {
                // Password does not match
                return -1;
            }
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



        public async Task<User> AuthenticateAsync(string staff_ID)
        {
            // SQL query to check if Staff_ID exists in the database and has a Lab_role of B2 or C3
            var sql = @"
            SELECT *
            FROM [User]
            WHERE Staff_ID = @Staff_ID AND (Lab_role = 'B2' OR Lab_role = 'C3')";

            // Execute the query and retrieve the user information
            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Staff_ID = staff_ID });

            // Return the authenticated user
            return user;
        }

        public async Task<User> AdminAuthenticateAsync(string staff_ID, string password)
        {
            // SQL query to retrieve user information based on Staff_ID
            var sql = @"
            SELECT *
            FROM [User]
            WHERE Staff_ID = @Staff_ID";

            // Execute the query and retrieve the user information
            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Staff_ID = staff_ID });

            // If user is found, verify the password
            if (user != null && VerifyPassword(password, user.Password))
            {
                return user;
            }

            // If authentication fails, return null
            return null;
        }

        // Helper method to verify the password
        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            var parts = storedHashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "bcrypt" || parts[2] != "v=2" || parts[3] != "rounds=10")
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[4]);
            string hashedInputPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: inputPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashedInputPassword == parts[5];
        }

        public async Task<int> ApproveUserAsync(string staff_ID)
        {
            string sql = @"
            BEGIN TRANSACTION;

            BEGIN TRY
                -- Update the user approval status and date
                UPDATE [User]
                SET ApprovalStatus = 1,
                    ApprovalDate = GETUTCDATE()  -- Use GETUTCDATE() for UTC time
                WHERE Staff_ID = @Staff_ID;

                -- Delete the corresponding notification
                DELETE FROM [Notification]
                WHERE Staff_ID = @Staff_ID;

                -- Commit the transaction if both operations succeed
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                -- Rollback the transaction if there is an error
                ROLLBACK TRANSACTION;
                -- Raise the error to the caller
                THROW;
            END CATCH";

            return await _connection.ExecuteAsync(sql, new { Staff_ID = staff_ID });
        }


        public async Task<int> StoreDeviceInfo(string Staff_ID, string DeviceID, string DeviceModel)
        {


            // Check if the Staff with provided Staff_ID exists
            var existingStaff = await _connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM [User] WHERE Staff_ID = @Staff_ID", new { Staff_ID = Staff_ID });

            if (existingStaff == null)
            {
                return 0;
            }
            else
            {

                // Call the stored procedure to add or update device info
                var rowsAffected = await _connection.ExecuteAsync("AddDeviceInfo",
                    new { DeviceID = DeviceID, Staff_ID = Staff_ID, DeviceModel = DeviceModel },
                    commandType: CommandType.StoredProcedure);
                return rowsAffected;
            }


        }

        /*        public async Task<decimal> GetUserLocationAsync(string StaffId)
                {
                    // Simulating fetching data from Flutter asynchronously
                    await Task.Delay(1000); // Simulate network delay

                    // For demonstration purposes, returning fixed values
                    decimal longitude = 45.1234m;
                    decimal latitude = -75.5678m;
                    var location=longitude+latitude;

                    return location;
                }
        */

        public async Task<IEnumerable<User>> GetNonApprovedAsync()
        {
            var sql = "SELECT * FROM [User] WHERE ApprovalStatus = 0";
            var nonApprovedUsers = await _connection.QueryAsync<User>(sql);
            return nonApprovedUsers;
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




    }
}
