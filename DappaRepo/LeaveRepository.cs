using Dapper;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace Employee_History.DappaRepo
{
    public class LeaveRepository:ILeaveRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;
        public LeaveRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task RequestLeaveAsync(string Staff_ID, DateTime startDate, DateTime endDate)
        {
            // Call the stored procedure to insert leave request
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            parameters.Add("@StartDate", startDate);
            parameters.Add("@EndDate", endDate);

            await _connection.ExecuteAsync("InsertLeaveRequest", parameters, commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsAsync()
        {
            var leaveRequests = await _connection.QueryAsync<LeaveRequest>("GetLeaveRequests", commandType: CommandType.StoredProcedure);
            return leaveRequests;
        }

        public async Task ApproveLeaveRequestAsync(string Staff_ID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", Staff_ID);
            await _connection.ExecuteAsync("ApproveLeaveRequest", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
