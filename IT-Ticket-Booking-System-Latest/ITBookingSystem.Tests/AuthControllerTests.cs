using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ITBookingSystem.Tests;

public class AuthControllerTests
{
    [Fact]
    public void Login_Get_WhenAnonymous_ReturnsView()
    {
        var authService = new AuthService(new StubUserRepository(), new ConfigurationBuilder().Build());
        var controller = new AuthController(authService, null!);
        TestHelpers.SetUser(controller, 1, UserRole.User, authenticated: false);

        var result = controller.Login();

        Assert.IsType<ViewResult>(result);
    }
}
