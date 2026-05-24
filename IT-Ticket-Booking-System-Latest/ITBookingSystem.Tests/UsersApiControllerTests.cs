using ITBookingSystem.Controllers.Api;
using ITBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class UsersApiControllerTests
{
    [Fact]
    public async Task List_ReturnsMappedUsers()
    {
        var repo = new StubUserRepository
        {
            OnGetAllAsync = _ => Task.FromResult(new List<User>
            {
                new() { Id = 1, Username = "u1", Email = "u1@test.com", Role = UserRole.Admin, IsActive = true }
            })
        };
        var controller = new UsersApiController(repo);

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
