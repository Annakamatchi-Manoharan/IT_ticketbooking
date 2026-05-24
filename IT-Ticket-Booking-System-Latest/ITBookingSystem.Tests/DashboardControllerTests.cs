using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_WhenAdmin_ReturnsView()
    {
        var dashboard = new DashboardService(new StubTicketRepository());
        var controller = new DashboardController(dashboard);
        TestHelpers.SetUser(controller, 99, UserRole.Admin);

        var result = await controller.Index(CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }
}
