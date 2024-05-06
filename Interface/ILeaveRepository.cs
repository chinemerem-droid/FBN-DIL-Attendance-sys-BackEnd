using Employee_History.Models;
namespace Employee_History.Interface
{
    public interface ILeaveRepository
    {
        public Task RequestLeaveAsync(string Staff_ID, DateTime startDate, DateTime endDate);
        public Task<IEnumerable<LeaveRequest>> GetLeaveRequestsAsync();
        public Task ApproveLeaveRequestAsync(string Staff_ID);
    }
}
