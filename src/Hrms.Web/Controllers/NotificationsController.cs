using Hrms.Notifications.Contracts;
using Hrms.Shared.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationQuery _notificationQuery;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(
        INotificationQuery notificationQuery,
        ICurrentUser currentUser)
    {
        _notificationQuery = notificationQuery;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Json(new { unreadCount = 0, items = Array.Empty<object>() });
        }

        var userId = _currentUser.UserId.Value;
        var unreadCount = await _notificationQuery.GetUnreadCountAsync(userId);
        var recent = await _notificationQuery.GetUserNotificationsAsync(userId, limit: 10);

        return Json(new
        {
            unreadCount,
            items = recent.Select(n => new
            {
                id = n.NotificationId,
                title = n.Title,
                message = n.Message,
                linkUrl = n.LinkUrl,
                isRead = n.IsRead,
                timeAgo = n.TimeAgo,
                createdAt = n.CreatedAtUtc.ToString("o")
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Json(new { success = false });
        }

        var userId = _currentUser.UserId.Value;
        await _notificationQuery.MarkAllAsReadAsync(userId);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Json(new { success = false });
        }

        var userId = _currentUser.UserId.Value;
        await _notificationQuery.MarkAsReadAsync(id, userId);
        return Json(new { success = true });
    }
}
