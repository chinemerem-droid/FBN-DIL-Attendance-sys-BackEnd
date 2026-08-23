using Dapper;
using Microsoft.Data.SqlClient;

namespace Employee_History.Features.Notifications
{
    /// <summary>
    /// One notification row: <c>{ id, staff_ID, roleID, isRead, message, name }</c>.
    /// RoleID is the audience (which admin role should see it).
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Staff_ID { get; set; } = string.Empty;
        public string? RoleID { get; set; }
        public bool IsRead { get; set; }
        public string? Message { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>Data access for admin notifications (approval requests, decisions).</summary>
    public interface INotificationRepository
    {
        /// <summary>All notifications addressed to a role, newest first, with the subject user's name.</summary>
        Task<IEnumerable<NotificationDto>> GetForRoleAsync(string roleId);
        Task<int> GetUnreadCountAsync(string roleId);
        Task<int> MarkReadAsync(int id);
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly SqlConnection _connection;

        public NotificationRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<NotificationDto>> GetForRoleAsync(string roleId)
        {
            return await _connection.QueryAsync<NotificationDto>(
                @"SELECT n.Id, n.Staff_ID, n.RoleID, n.IsRead, n.Message, u.Name
                  FROM [Notification] n
                  LEFT JOIN [User] u ON u.Staff_ID = n.Staff_ID
                  WHERE n.RoleID = @RoleID
                  ORDER BY n.Id DESC",
                new { RoleID = roleId });
        }

        public async Task<int> GetUnreadCountAsync(string roleId)
        {
            return await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [Notification] WHERE RoleID = @RoleID AND IsRead = 0",
                new { RoleID = roleId });
        }

        public async Task<int> MarkReadAsync(int id)
        {
            return await _connection.ExecuteAsync(
                "UPDATE [Notification] SET IsRead = 1 WHERE Id = @Id",
                new { Id = id });
        }
    }
}
