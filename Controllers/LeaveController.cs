using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        [HttpPost("request")]
        public async Task<IActionResult> RequestLeave([FromBody] LeaveRequest leave)
        {
            try
            {
                await leaveRepository.RequestLeaveAsync(leave.Staff_ID, leave.StartDate, leave.EndDate);
                return Ok("Leave request submitted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Authorize]
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
        [Authorize]
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLeaveRequest([FromBody] LeaveRequest leave)
        {
            try
            {
                await leaveRepository.ApproveLeaveRequestAsync(leave.Staff_ID);
                return Ok("Leave request approved successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
