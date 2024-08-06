using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using Employee_History.Models;
using Employee_History.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            var currentDate = DateTime.Now.Date;
            var entryTime = DateTime.Now.TimeOfDay;
            var statusTime = DateTime.Now.TimeOfDay;

            var existingEntry = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                @"SELECT * 
          FROM Attendance_History
          WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                new { Staff_ID, currentDate });

            string status = statusTime > new TimeSpan(11, 0, 0) ? "LATE" : "ON TIME";

            if (existingEntry != null)
            {
                // Update the existing record
                await _connection.ExecuteAsync(
                    @"UPDATE Attendance_History 
              SET EntryTime = @entryTime, CheckinStatus = @status
              WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, entryTime, currentDate, status});

                // Return the updated record with converted times
                var updatedEntry = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                    @"SELECT * 
              FROM Attendance_History 
              WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, currentDate });

                // Create a new instance with converted times
                return new Attendance_History
                {
                    Staff_ID = updatedEntry.Staff_ID,
                    EntryTime = updatedEntry.EntryTime,
                    ExitTime = updatedEntry.ExitTime,
                    Date = updatedEntry.Date,
                    Month = updatedEntry.Month,
                    Year = updatedEntry.Year,
                    CheckinStatus = updatedEntry.CheckinStatus
                };
            }
            else
            {
                // Insert a new record
                await _connection.ExecuteAsync(
                    @"INSERT INTO Attendance_History (Staff_ID, EntryTime, ExitTime, Date,CheckinStatus)
              VALUES (@Staff_ID, @entryTime, NULL, @currentDate,@status)",
                    new { Staff_ID, entryTime, currentDate,status });

                // Return the newly created record with converted times
                var newEntry = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(
                    @"SELECT * 
              FROM Attendance_History 
              WHERE Staff_ID = @Staff_ID AND Date = @currentDate",
                    new { Staff_ID, currentDate });

                // Create a new instance with converted times
                return new Attendance_History
                {
                    Staff_ID = newEntry.Staff_ID,
                    EntryTime = newEntry.EntryTime,
                    ExitTime = newEntry.ExitTime,
                    Date = newEntry.Date,
                    Month = newEntry.Month,
                    Year = newEntry.Year,
                    CheckinStatus = newEntry.CheckinStatus
                };
            }
        }

        public async Task<Attendance_History> Checkout(string staff_ID)
        {
            try
            {
                var currentDate = DateTime.Now.Date;
                var exitTime = DateTime.Now.TimeOfDay;

                var rowsAffected = await _connection.ExecuteAsync(@"
          UPDATE Attendance_History
          SET ExitTime = @exitTime
          WHERE Staff_ID = @staff_ID AND CAST(Date AS DATE) = @currentDate",
                  new { staff_ID, currentDate, exitTime });

                if (rowsAffected == 0)
                {
                    return null;
                }

                var attendanceHistory = await _connection.QueryFirstOrDefaultAsync<Attendance_History>(@"
          SELECT *
          FROM Attendance_History
          WHERE Staff_ID = @staff_ID AND Date = @currentDate",
                  new { staff_ID, currentDate });

                // Create a new instance with converted times
                return new Attendance_History
                {
                    Staff_ID = attendanceHistory.Staff_ID,
                    EntryTime = attendanceHistory.EntryTime,
                    ExitTime = attendanceHistory.ExitTime,
                    Date = attendanceHistory.Date,
                    Month = attendanceHistory.Month,
                    Year = attendanceHistory.Year,
                    CheckinStatus = attendanceHistory.CheckinStatus
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }


        public async Task<IEnumerable<Attendance_History>> GetLateCheckinStaffAsync()
        {
            return await _connection.QueryAsync<Attendance_History>("GetLateCheckinStaff", commandType: CommandType.StoredProcedure);
        }
    }
}
