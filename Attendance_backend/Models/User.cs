using Microsoft.AspNetCore.Identity;

namespace Attendance_backend.Models
{
    public class User:IdentityUser
    {
        public int UserID { get; set; }
        public int RoleID { get; set; }
        public string StaffID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
    }
}
