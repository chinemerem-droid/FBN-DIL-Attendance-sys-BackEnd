using Employee_History.DappaRepo;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : Controller
    {
        private readonly ILeaveRepository leaveRepository;
        public LeaveController(ILeaveRepository leaveRepository)
        {
            this.leaveRepository = leaveRepository;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestLeave(string Staff_ID, DateTime startDate, DateTime endDate)
        {
            try
            {
                await leaveRepository.RequestLeaveAsync(Staff_ID, startDate, endDate);
                return Ok("Leave request submitted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Getrequests")]
        public async Task<IActionResult> GetLeaveRequests()
        {
            try
            {
                var leaveRequests = await leaveRepository.GetLeaveRequestsAsync();
                return Ok(leaveRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLeaveRequest(string Staff_ID)
        {
            try
            {
                await leaveRepository.ApproveLeaveRequestAsync(Staff_ID);
                return Ok("Leave request approved successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
