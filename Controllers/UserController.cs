using Employee_History.DappaRepo;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> AddUser(string StaffID, string Name, string Email, string Device, long Phone_number, string Lab_role, string Password)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.AddUser(StaffID, Name, Email, Device, Phone_number, Lab_role, Password);

                // Check if the user is added successfully
                if (user != null)
                {
                    // Return success message
                    return Ok("User added successfully");
                }
                else
                {
                    // Return appropriate error message if user addition fails
                    return BadRequest("Failed to add user");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for troubleshooting
                Console.WriteLine(ex);
                // Return appropriate error message if an exception occurs
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPost("RemoveUser")]
        public async Task<IActionResult> RemoveUser(string StaffID)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.RemoveUser(StaffID);

                // Check if the user is added successfully
                if (user == null)
                {
                    return Ok("User Removed successfully");
                }
                else
                {                 
                    return BadRequest("Failed to Remove user");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

       
    }
}
