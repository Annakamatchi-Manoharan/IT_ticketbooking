using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ITBookingSystem.Tests;

public class NotificationsControllerTests
{
    [Fact]
    public async Task UnreadCount_ReturnsCountJson()
    {
        var notifications = new StubNotificationRepository
        {
            OnCountUnreadAsync = (_, _) => Task.FromResult(7)
        };
        var controller = new NotificationsController(notifications, new NullLogger<NotificationsController>());
        TestHelpers.SetUser(controller, 42, UserRole.User);

        var result = await controller.UnreadCount(CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var count = json.Value!.GetType().GetProperty("count")!.GetValue(json.Value);
        Assert.Equal(7, count);
    }
}
