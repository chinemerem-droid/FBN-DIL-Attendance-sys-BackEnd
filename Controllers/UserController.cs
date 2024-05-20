using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Reflection.Metadata.Ecma335;
using Employee_History.DappaRepo;
using Microsoft.AspNetCore.Identity.Data;

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
        public UserController(IDapperUser dapperUser, LocationRange locationRange, IConfiguration configuration,IDappaEmployee dappaEmployee)
        {
            this.dapperUser = dapperUser;
            this.dappaEmployee = dappaEmployee;
            _configuration = configuration;
            this.locationRange = locationRange;
        }
        [HttpPost("checkin")]
        public async Task<IActionResult> Checkin(string Staff_ID, string DeviceID, string DeviceModel, decimal longitude, decimal latitude)
        {
            // Retrieve user information
            var user = await dapperUser.AuthenticateAsync(Staff_ID);
            if (user == null)
            {
                return BadRequest("Invalid Staff ID");
            }

            // Check if device info matches the stored info
            if (user.DeviceID != DeviceID || user.DeviceModel != DeviceModel)
            {
                return BadRequest("Device information does not match.");
            }

            // Check if location is within acceptable range
            if (IsLocationInRange(longitude, latitude))
            {
                return BadRequest("Location is not within acceptable range.");
            }

            // If all checks pass, proceed with check-in
            var attendanceHistory = await dappaEmployee.Checkin(Staff_ID);
            return Ok(attendanceHistory);
        }

        private bool IsLocationInRange(decimal longitude, decimal latitude)
        {
            // Use injected LocationRange values
            return longitude >= locationRange.MinLongitude && longitude <= locationRange.MaxLongitude &&
                   latitude >= locationRange.MinLatitude && latitude <= locationRange.MaxLatitude;
        }



        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.AddUser(Staff_ID, Name, Email, Phone_number, Lab_role);
                return Ok("User added succesfully");


            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPost("RemoveUser")]
        public async Task<IActionResult> RemoveUser(string Staff_ID)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.RemoveUser(Staff_ID);
                return Ok("User removed successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpGet("AddedUsers")]
        public async Task<IEnumerable<User>> GetUsers()
        {
            try
            {
                return await dapperUser.GetUsers();
            }
            catch
            {
                throw;
            }
        }
        [HttpPost("AddPassword")]
        public async Task<IActionResult> AddPassword(string Staff_ID, string Password)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(Staff_ID) || string.IsNullOrEmpty(Password))
                {
                    return BadRequest("Staff ID and password are required.");
                }

                // Call the repository method to add the user
                var user = await dapperUser.AddPassword(Staff_ID, Password);
                if (user != null)
                {
                    return Ok("User password added successfully.");
                }
                else
                {
                    return StatusCode(500, "Failed to add user password.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception


                // Return appropriate error response
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("ConfirmPassword")]
        public async Task<IActionResult> ConfirmPassword(string Staff_ID, string Password)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(Staff_ID) || string.IsNullOrEmpty(Password))
                {
                    return BadRequest("Staff ID and password are required.");
                }

                // Call the repository method to confirm the password
                int result = await dapperUser.ConfirmPassword(Staff_ID, Password);

                // Check the result
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

                // Return appropriate error response
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string Staff_ID, string DeviceID, string DeviceModel)
        {
            var user = await dapperUser.AuthenticateAsync(Staff_ID);

            if (user == null)
            {
                return BadRequest("Invalid Staff ID");
            }
            var isApproved = await dapperUser.IsUserApprovedAsync(Staff_ID);
            if (!isApproved)
            {
                return BadRequest("User is not approved.");
            }

            // If user is approved, store device info
            await dapperUser.StoreDeviceInfo(Staff_ID, DeviceID, DeviceModel);

            return Ok("Login successful and device info stored.");
        }


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
                // Log the exception
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveUser(string staff_ID)
        {
            try
            {
                var rowsAffected = await dapperUser.ApproveUserAsync(staff_ID);
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
                // Log the exception
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("ApprovalHistory")]
        public async Task<IActionResult> GetApprovalData(int daysBehind)
        {
            var approvalData = await dapperUser.GetApprovalDataAsync(daysBehind);
            return Ok(approvalData);
        }

        [HttpGet("DeletionHistory")]
        public async Task<IActionResult> GetRemovalData(int daysBehind)
        {
            var removalData = await dapperUser.GetRemovalDataAsync(daysBehind);
            return Ok(removalData);
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



    }
}
