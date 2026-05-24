using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ITBookingSystem.Tests;

public class HomeControllerTests
{
    [Fact]
    public void Index_WhenUnauthenticated_RedirectsToLogin()
    {
        var controller = new HomeController(new NullLogger<HomeController>());
        TestHelpers.SetUser(controller, 1, UserRole.User, authenticated: false);

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Auth", redirect.ControllerName);
    }
}
