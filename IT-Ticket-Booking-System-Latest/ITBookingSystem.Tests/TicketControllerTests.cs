using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class TicketControllerTests
{
    [Fact]
    public async Task AddComment_WhenMessageEmpty_RedirectsToDetails()
    {
        var controller = new TicketController(null!, new StubUserRepository(), new StubTicketRepository(), new StubTicketHistoryRepository(), null!);
        TestHelpers.SetUser(controller, 5, UserRole.User);

        var result = await controller.AddComment(11, "   ", CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
    }
}
