﻿using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IDapperUser
    {
        public Task<User> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role);
        public Task<User> RemoveUser(string Staff_ID);
        public Task<IEnumerable<User>> GetUsers();
        public Task<User> AddPassword(string Staff_ID, string Password);

        public Task<int> ConfirmPassword(string Staff_ID, string Password);

        Task<bool> IsUserApprovedAsync(string staff_ID);
        Task<User> AuthenticateAsync(string staff_ID, string password);
        public Task<IEnumerable<User>> GetNonApprovedAsync();
        Task<int> ApproveUserAsync(string staff_ID);
    }
}
