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
            return await _connection.QueryAsync<Attendance_History>("AllAttendance", commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<Attendance_History> GetAttendanceByID(string Staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);

            return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(@"AttById", parameters, commandType: System.Data.CommandType.StoredProcedure);

        }
        public async Task<IEnumerable<Attendance_History>> GetAttendanceByDate(DateTime Date)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@StartDate", Date);

            return await _connection.QueryAsync<Attendance_History>(@"AttByDate", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<Attendance_History> GetAttendanceByIDandDate(string Staff_ID, DateTime Date)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@Date", Date);

            return await _connection.QueryFirstOrDefaultAsync<Attendance_History>(@"GetAttByStaffAndDate", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string Staff_ID, DateTime StartDate, DateTime EndDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@StartDate", StartDate);
            parameters.Add("@EndDate", EndDate);

            // Return a list of Attendance_History objects
            return await _connection.QueryAsync<Attendance_History>(@"AttByIdANdDateRange", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetAttendancebtwDates(DateTime startDate, DateTime endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@startDate", startDate);
            parameters.Add("@endDate", endDate);

            // Return a list of Attendance_History objects
            return await _connection.QueryAsync<Attendance_History>(@"AttByDateRange", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<Attendance_History> Checkin(string Staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            return await _connection.QueryFirstOrDefaultAsync<Attendance_History>("AddCheckIn", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }
        public async Task Checkout(string staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", staff_ID);

            await _connection.ExecuteAsync("AddCheckOut", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Attendance_History>> GetLateCheckinStaffAsync()
        {
            return await _connection.QueryAsync<Attendance_History>("GetLateCheckinStaff", commandType: System.Data.CommandType.StoredProcedure);
        }




    }
}
