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
        public async Task<IActionResult> AddUser(string Staff_ID, string Name, string Email, string Device, long Phone_number, string Lab_role, string Password)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dapperUser.AddUser(Staff_ID, Name, Email, Device, Phone_number, Lab_role, Password);
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

        [HttpGet("Added Users")]
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

    }
}
