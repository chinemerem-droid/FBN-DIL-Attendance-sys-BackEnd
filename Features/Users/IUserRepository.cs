using Employee_History.Common.Models;

namespace Employee_History.Features.Users
{
    /// <summary>Data access for the user lifecycle: create, list, approve, deny, remove, history.</summary>
    public interface IUserRepository
    {
        /// <summary>Creates a user (and an A1 approval notification). Throws <see cref="DuplicateStaffIdException"/> if the staff id exists.</summary>
        Task AddUserAsync(AddUserRequest request);
        Task<User?> GetByStaffIdAsync(string staffId);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<PagedResult<UserDto>> GetPagedAsync(string? query, int page, int pageSize);
        Task<IEnumerable<UserDto>> GetNonApprovedAsync();
        Task<IEnumerable<UserDto>> GetByRoleAsync(string labRole);
        Task<int> ApproveAsync(string staffId);
        Task<int> DenyAsync(string staffId);
        /// <summary>Soft-removes a user (ApprovalStatus = 0, RemovalDate set) and revokes their sessions.</summary>
        Task<int> RemoveAsync(string staffId);
        Task<IEnumerable<ApprovalRecordDto>> GetApprovalHistoryAsync();
        Task<IEnumerable<RemovalRecordDto>> GetRemovalHistoryAsync();
        /// <summary>Clears a user's approval/removal dates, removing them from the history views.</summary>
        Task<int> ClearHistoryDatesAsync(string staffId);
        /// <summary>Clears the device binding so the user can register a new device on next login.</summary>
        Task<int> ResetDeviceAsync(string staffId);
    }
}
