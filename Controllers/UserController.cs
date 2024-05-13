using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IDapperUser dapperUser;
        public UserController(IDapperUser dapperUser)
        {
            this.dapperUser = dapperUser;
        }

        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.AddUser(Staff_ID, Name, Email,Phone_number, Lab_role);
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
        public async Task<IActionResult> Login(string staff_ID, string generatedCode)
        {
            // Authenticate user
            var user = await dapperUser.AuthenticateAsync(staff_ID, generatedCode);

            if (user == null)
            {
                return Unauthorized(); // Return 401 Unauthorized if authentication fails
            }

            // Check if user is approved
            var isApproved = await dapperUser.IsUserApprovedAsync(staff_ID);
            if (!isApproved)
            {
                return Forbid(); // Return 403 Forbidden if user is not approved
            }


            return Ok("Successfull login");
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

        [HttpPost("GenerateCode")]
        public async Task<string> InsertGeneratedCode(string Staff_ID)
        {
            var generatedCode = await dapperUser.InsertGeneratedCode(Staff_ID, HttpContext);
            return generatedCode; // Or other appropriate IActionResult based on your needs
        }


    }
}
