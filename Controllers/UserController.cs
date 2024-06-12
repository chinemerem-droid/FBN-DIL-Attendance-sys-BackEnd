using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Data;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IDapperUser dapperUser;
        private readonly IDappaEmployee dappaEmployee;
        private readonly LocationRange locationRange;
        private readonly IConfiguration _configuration;
        private readonly string secretKey;

        public UserController(IDapperUser dapperUser, LocationRange locationRange, IConfiguration configuration, IDappaEmployee dappaEmployee)
        {
            this.dapperUser = dapperUser;
            this.dappaEmployee = dappaEmployee;
            _configuration = configuration;
            this.locationRange = locationRange;
        }

        [Authorize]
        [HttpPost("checkin")]
        public async Task<IActionResult> Checkin([FromBody] User userModel)
        {
            try
            {
                var user = await dapperUser.AuthenticateAsync(userModel.Staff_ID);
                if (user == null)
                {
                    return BadRequest("Invalid Staff ID");
                }

                if (user.DeviceID != userModel.DeviceID || user.DeviceModel != userModel.DeviceModel)
                {
                    return BadRequest("Device information does not match.");
                }

                if (!IsLocationInRange(userModel.Longitude, userModel.Latitude))
                {
                    return BadRequest("Location is not within acceptable range.");
                }

                var attendanceHistory = await dappaEmployee.Checkin(userModel.Staff_ID);
                if (attendanceHistory == null)
                {
                    return StatusCode(500, "Failed to record check-in. Please try again.");
                }

                return Ok("Check-in successful");
            }
            catch (Exception ex)
            {
                // Log the exception for troubleshooting
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }


        private bool IsLocationInRange(decimal longitude, decimal latitude)
        {
            var minLongitude = _configuration.GetValue<decimal>("LocationRange:MinLongitude");
            var maxLongitude = _configuration.GetValue<decimal>("LocationRange:MaxLongitude");
            var minLatitude = _configuration.GetValue<decimal>("LocationRange:MinLatitude");
            var maxLatitude = _configuration.GetValue<decimal>("LocationRange:MaxLatitude");

            return longitude >= minLongitude && longitude <= maxLongitude &&
                   latitude >= minLatitude && latitude <= maxLatitude;
        }

        [Authorize]
        [HttpGet("GetNotification")]
        public async Task<IEnumerable<Notification>> GetNotification()
        {
            try
            {
                return await dapperUser.GetNotification();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [Authorize]
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] User userModel)
        {
            try
            {
                await dapperUser.AddUser(userModel.Staff_ID, userModel.Name, userModel.Email, userModel.Phone_number, userModel.Lab_role);
                return Ok("User added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [Authorize]
        [HttpPost("RemoveUser")]
        public async Task<IActionResult> RemoveUser([FromBody] User userModel)
        {
            try
            {
                await dapperUser.RemoveUser(userModel.Staff_ID);
                return Ok("User removed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [Authorize]
        [HttpGet("AddedUsers")]
        public async Task<IEnumerable<User>> GetUsers()
        {
            try
            {
                return await dapperUser.GetUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [Authorize]
        [HttpPost("ConfirmPassword")]
        public async Task<IActionResult> ConfirmPassword([FromBody] User userModel)
        {
            try
            {
                if (string.IsNullOrEmpty(userModel.Staff_ID) || string.IsNullOrEmpty(userModel.Password))
                {
                    return BadRequest("Staff ID and password are required.");
                }

                int result = await dapperUser.ConfirmPassword(userModel.Staff_ID, userModel.Password);

                if (result == 0)
                {
                    return Ok("Password confirmed successfully.");
                }
                else if (result == -1)
                {
                    return BadRequest("Incorrect password or user not found.");
                }
                else
                {
                    return StatusCode(500, "An error occurred while confirming the password.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("loginuser")]
        public async Task<IActionResult> Login([FromBody] User userModel)
        {
            try
            {
                if (string.IsNullOrEmpty(userModel.Staff_ID) || string.IsNullOrEmpty(userModel.DeviceID) || string.IsNullOrEmpty(userModel.DeviceModel))
                {
                    return BadRequest("Staff ID, Device ID, and Device Model are required.");
                }

                var user = await dapperUser.AuthenticateAsync(userModel.Staff_ID);
                if (user == null)
                {
                    return BadRequest("Invalid Staff ID");
                }

                var isApproved = await dapperUser.IsUserApprovedAsync(userModel.Staff_ID);
                if (!isApproved)
                {
                    return BadRequest("User is not approved.");
                }

                await dapperUser.StoreDeviceInfo(userModel.Staff_ID, userModel.DeviceID, userModel.DeviceModel);

                var token = JwtTokenGenerator.GenerateToken(user, _configuration);

                return Ok(new { message = "Login successful and device info stored.", token });
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred during login: " + ex.Message);
            }
        }

        [HttpPost("loginAdmin")]
        public async Task<IActionResult> LoginAdmin([FromBody] User userModel)
        {
            var user = await dapperUser.AdminAuthenticateAsync(userModel.Staff_ID, userModel.Password);

            if (user == null)
            {
                return BadRequest("Invalid Staff ID, Password, or insufficient role.");
            }

            var token = JwtTokenGenerator.GenerateToken(user, _configuration);

            return Ok(new { message = "Login successful", token });
        }



        [Authorize]
        [HttpGet("nonapproved")]
        public async Task<IActionResult> GetNonApprovedUsers()
        {
            try
            {
                var nonApprovedUsers = await dapperUser.GetNonApprovedAsync();
                return Ok(nonApprovedUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [Authorize]
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveUser([FromBody] User userModel)
        {
            try
            {
                var rowsAffected = await dapperUser.ApproveUserAsync(userModel.Staff_ID);
                if (rowsAffected > 0)
                {
                    return Ok("User approval status updated successfully.");
                }
                else
                {
                    return NotFound("User not found or already approved.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [Authorize]
        [HttpGet("ApprovalHistory")]
        public async Task<IActionResult> GetApprovalData(int daysBehind)
        {
            try
            {
                var approvalData = await dapperUser.GetApprovalDataAsync(daysBehind);
                return Ok(approvalData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [Authorize]
        [HttpGet("DeletionHistory")]
        public async Task<IActionResult> GetRemovalData(int daysBehind)
        {
            try
            {
                var removalData = await dapperUser.GetRemovalDataAsync(daysBehind);
                return Ok(removalData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("employeesByRole")]
        public async Task<IActionResult> GetEmployeesByRoleID(string Lab_role)
        {
            try
            {
                var employees = await dapperUser.GetEmployeesByRoleIDAsync(Lab_role);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
