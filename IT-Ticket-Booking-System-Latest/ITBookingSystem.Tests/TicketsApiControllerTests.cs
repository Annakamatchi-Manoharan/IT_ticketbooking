using ITBookingSystem.Controllers.Api;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

public class TicketsApiControllerTests
{
    [Fact]
    public async Task List_WhenAdmin_ReturnsOk()
    {
        var repo = new StubTicketRepository
        {
            OnGetAllPagedAsync = (_, _, _) => Task.FromResult(new PagedResult<TicketListItemDto> { Items = new List<TicketListItemDto>() })
        };
        var controller = new TicketsApiController(repo);
        TestHelpers.SetUser(controller, 1, UserRole.Admin);

        var result = await controller.List(1, 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
