using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Employee_History.Features.Leave
{
    /// <summary>Data access for leave requests (stored procedures).</summary>
    public interface ILeaveRepository
    {
        Task RequestLeaveAsync(string staffId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsAsync();
        Task ApproveLeaveRequestAsync(string staffId);
    }

    public class LeaveRepository : ILeaveRepository
    {
        private readonly SqlConnection _connection;

        public LeaveRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task RequestLeaveAsync(string staffId, DateTime startDate, DateTime endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", staffId);
            parameters.Add("@StartDate", startDate);
            parameters.Add("@EndDate", endDate);

            await _connection.ExecuteAsync("InsertLeaveRequest", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsAsync()
        {
            return await _connection.QueryAsync<LeaveRequestDto>("GetLeaveRequests", commandType: CommandType.StoredProcedure);
        }

        public async Task ApproveLeaveRequestAsync(string staffId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", staffId);
            await _connection.ExecuteAsync("ApproveLeaveRequest", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
