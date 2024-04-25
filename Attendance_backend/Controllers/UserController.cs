using Microsoft.AspNetCore.Identity; // Assuming Identity is used for user management
using Microsoft.AspNetCore.Mvc;
using Attendance_backend.Models;

namespace Attendance_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager; // Assuming User inherits from IdentityUser

        public UserController(UserManager<User> userManager) // Inject UserManager through dependency injection
        {
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userManager.CreateAsync(user); // Assuming User inherits from IdentityUser
            if (result.Succeeded)
            {
                return Ok("User added successfully");
            }

            return BadRequest(result.Errors); // Handle creation errors
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Ok("User removed successfully");
            }

            return BadRequest(result.Errors); // Handle deletion errors
        }
    }
}
