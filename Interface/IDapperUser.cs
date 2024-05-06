using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IDapperUser
    {
        public Task<User> AddUser(string Staff_ID, string Name, string Email, string Device, long Phone_number, string Lab_role, string Password);
        public Task<User> RemoveUser(string Staff_ID);
        public Task<IEnumerable<User>> GetUsers();
    }
}
