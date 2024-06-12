using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Employee_History.Models;
using Dapper;
using System.Data;
using Employee_History.Interface;

namespace Employee_History.DappaRepo
{
    public class DappaEmployee : IDappaEmployee
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;
        public DappaEmployee(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendance()
        {
            return await _connection.QueryAsync<Attendance_History>("AllAttendance", commandType: CommandType.StoredProcedure);
        }

        public async Task<Attendance_History> GetAttendanceByID(string Staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);

            return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(@"AttById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendanceByDate(DateTime Date)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@StartDate", Date);

            return await _connection.QueryAsync<Attendance_History>(@"AttByDate", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<Attendance_History> GetAttendanceByIDandDate(string Staff_ID, DateTime Date)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Date", Date);

            return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(@"GetAttByStaffAndDate", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string Staff_ID, DateTime StartDate, DateTime EndDate)
        {
            var query = @"
                SELECT * FROM Attendance_History 
                WHERE Staff_ID = @Staff_ID AND Date >= @startDate AND Date <= @endDate";

            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@startDate", StartDate);
            parameters.Add("@endDate", EndDate);

            var result = await _connection.QueryAsync<Attendance_History>(query, parameters);
            return result;
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendancebtwDates(DateTime StartDate, DateTime EndDate)
        {
            var query = @"
                SELECT * FROM Attendance_History 
                WHERE Date >= @startDate AND Date <= @endDate";

            var parameters = new DynamicParameters();
            parameters.Add("@startDate", StartDate);
            parameters.Add("@endDate", EndDate);

            var result = await _connection.QueryAsync<Attendance_History>(query, parameters);
            return result;
        }

        public async Task<Attendance_History> Checkin(string Staff_ID)
        {
            // Get current date and time
            var currentDate = DateTime.Now.Date;
            var entryTime = DateTime.Now.TimeOfDay;

            // Check for existing entry today
            var existingEntry = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                @"SELECT * 
                FROM Attendance_History
                WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                new { Staff_ID, currentDate });

            // Set the status based on entry time
            string status;
            if (entryTime > new TimeSpan(11, 0, 0))
            {
                status = "LATE";
            }
            else
            {
                status = "ON TIME";
            }

            if (existingEntry != null)
            {
                // If there's already a check-in for today, update the existing record
                await _connection.ExecuteAsync(
                    @"UPDATE Attendance_History 
            SET EntryTime = @entryTime, CheckinStatus = @status 
            WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, entryTime, currentDate, status });

                // Return the updated record
                return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                    @"SELECT * 
                    FROM Attendance_History 
                    WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, currentDate });
            }
            else
            {
                // If no existing check-in for today, insert a new record
                await _connection.ExecuteAsync(
                    @"INSERT INTO Attendance_History (Staff_ID, EntryTime, ExitTime, Date, CheckinStatus)
            VALUES (@Staff_ID, @entryTime, NULL, @currentDate, @status)",
                    new { Staff_ID, entryTime, currentDate, status });

                // Retrieve the newly inserted record
                return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                    @"SELECT * 
            FROM Attendance_History 
            WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, currentDate });
            }
        }

        /* public async Task<Attendance_History> Checkin(string Staff_ID)
         {
             // Get current date and time
             var currentDate = DateTime.Now.Date;
             var entryTime = DateTime.Now.TimeOfDay;

             // Check for existing entry today
             var existingEntry = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                 @"SELECT * 
        FROM Attendance_History
        WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                 new { Staff_ID, currentDate });

             if (existingEntry != null)
             {
                 throw new Exception("Staff ID already has a check-in for today.");
             }

             // Set the status based on entry time
             string status;
             if (entryTime > new TimeSpan(11, 0, 0))
             {
                 status = "LATE";
             }
             else
             {
                 status = "ON TIME";
             }

             // Insert new check-in data
             await _connection.ExecuteAsync(
                 @"INSERT INTO Attendance_History (Staff_ID, EntryTime, ExitTime, Date, CheckinStatus)
        VALUES (@Staff_ID, @entryTime, NULL, @currentDate, @status)",
                 new { Staff_ID, entryTime, currentDate, status });

             // Assuming Attendance_History has an auto-incrementing ID
             // Retrieve the newly inserted record
             return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                 @"SELECT * FROM Attendance_History WHERE Staff_ID = @Staff_ID AND Date = @currentDate", new { Staff_ID, currentDate });
         }
 */

        public async Task Checkout(string staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", staff_ID);

            await _connection.ExecuteAsync("AddCheckOut", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetLateCheckinStaffAsync()
        {
            return await _connection.QueryAsync<Attendance_History>("GetLateCheckinStaff", commandType: CommandType.StoredProcedure);
        }
    }
}
