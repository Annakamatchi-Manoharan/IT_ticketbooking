using ITBookingSystem.Controllers.Api;
using ITBookingSystem.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class AuthApiControllerTests
{
    [Fact]
    public async Task Token_WhenModelInvalid_ReturnsValidationProblem()
    {
        var controller = new AuthApiController(null!);
        controller.ModelState.AddModelError("Email", "Required");

        var result = await controller.Token(new LoginDto(), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }
}
