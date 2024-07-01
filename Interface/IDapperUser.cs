using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Interface
{
    public interface IDapperUser
    {
        public Task<IEnumerable<Notification>> GetNotification();
        public Task<int> ReadAsync(string Staff_ID,string RoleID);
        public Task<int> DenyUserAsync(string staff_ID);
        public Task<bool> AddUser(string Staff_ID, string Name, string Email, long Phone_number, string Lab_role);
        public Task<int> RemoveUser(string Staff_ID);
        public Task<IEnumerable<User>> GetUsers();
        public Task<User> AddPassword(string Staff_ID, string Password);

        public Task<int> ConfirmPassword(string Staff_ID, string Password);

        Task<bool> IsUserApprovedAsync(string staff_ID);
        public Task<User> AdminAuthenticateAsync(string staff_ID,string Password);
        public Task<User> AuthenticateAsync(string staff_ID);
        public Task<IEnumerable<User>> GetNonApprovedAsync();
        Task<int> ApproveUserAsync(string staff_ID);
        Task<IEnumerable<User>> GetApprovalDataAsync();
        public Task<int> SetDatesToNullAsync(int staffId);
        Task<IEnumerable<User>> GetRemovalDataAsync();
        Task<IEnumerable<User>> GetEmployeesByRoleIDAsync(string Lab_role);
        public Task<int> StoreDeviceInfo(string Staff_ID, string DeviceID, string DeviceModel);
        public Task RequestPasswordResetAsync(string email);
        public Task<bool> VerifyPasswordResetTokenAsync(string email, string token, string newPassword);

        /* public Task<decimal> GetUserLocationAsync(string StaffId);*/
    }
}
