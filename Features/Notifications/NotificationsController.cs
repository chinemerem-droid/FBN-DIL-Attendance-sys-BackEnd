using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Notifications
{
    /// <summary>
    /// Admin notifications (pending approvals, approval/denial outcomes).
    /// Notifications are addressed to a role; each caller sees the ones for
    /// their own role. Admin-only (A1/B2). Legacy routes under /api/User/*
    /// are preserved for the existing frontend.
    /// </summary>
    [Route("api/User")]
    [Authorize(Policy = "Admin")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationRepository _notifications;

        public NotificationsController(INotificationRepository notifications)
        {
            _notifications = notifications;
        }

        /// <summary>Lists the caller's role-addressed notifications, newest first.</summary>
        /// <remarks>Expects: bearer token only. Returns: 200 with [{ id, staff_ID, roleID, isRead, message, name }].</remarks>
        [HttpGet("GetNotification")]
        public async Task<IActionResult> GetNotifications()
        {
            var role = CallerRole ?? "A1";
            return Ok(await _notifications.GetForRoleAsync(role));
        }

        /// <summary>Unread-notification count for the caller's role (badge counter).</summary>
        /// <remarks>Expects: bearer token only. Returns: 200 { count }.</remarks>
        [HttpGet("Notification/count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var role = CallerRole ?? "A1";
            var count = await _notifications.GetUnreadCountAsync(role);
            return Ok(new { count });
        }

        /// <summary>Marks one notification as read.</summary>
        /// <remarks>Expects: notification id in the URL. Returns: 200 { success, message }; 404 if the id does not exist.</remarks>
        [HttpPut("Notification/{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var rows = await _notifications.MarkReadAsync(id);
            if (rows == 0)
            {
                return NotFound(new ApiMessage("Notification not found.", false));
            }
            return Ok(new ApiMessage("Notification marked as read."));
        }
    }
}
