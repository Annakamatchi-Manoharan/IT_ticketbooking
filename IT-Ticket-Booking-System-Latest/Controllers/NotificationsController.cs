using System.Security.Claims;
using ITBookingSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationRepository _notifications;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationRepository notifications, ILogger<NotificationsController> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var count = await _notifications.CountUnreadAsync(userId, ct);
            return Json(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get unread notification count for user {UserId}", userId);
            return Json(new { count = 0 });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var items = await _notifications.GetRecentForUserAsync(userId, 20, ct);
            return PartialView("_NotificationList", items);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load notifications for user {UserId}", userId);
            return PartialView("_NotificationList", new List<ITBookingSystem.Models.InAppNotification>());
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notifications.MarkReadAsync(id, userId, ct);
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notifications.MarkAllReadAsync(userId, ct);
        return Ok();
    }
}
