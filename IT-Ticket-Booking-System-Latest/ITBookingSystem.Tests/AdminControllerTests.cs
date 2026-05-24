using ITBookingSystem.Controllers;
using ITBookingSystem.Models;
using ITBookingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task Users_WhenNoEditProvided_ReturnsDefaultModel()
    {
        var controller = new AdminController(new StubUserRepository(), new StubTicketRepository(), null!);
        TestHelpers.SetUser(controller, 1, UserRole.Admin);

        var result = await controller.Users(null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserEditVm>(view.Model);
        Assert.Equal(UserRole.User, model.Role);
    }
}
