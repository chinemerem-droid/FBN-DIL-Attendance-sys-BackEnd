using Employee_History.Models;

namespace Employee_History.DappaRepo
{
    public interface IDapperUser
    {
        public Task<User> AddUser( string StaffID, string Name, string Email, string Device, long Phone_number, string Lab_role, string Password);
        public Task<User> RemoveUser(string StaffID);
    }
}
