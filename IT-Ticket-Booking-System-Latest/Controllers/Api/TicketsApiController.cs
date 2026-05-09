using System.Security.Claims;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers.Api;

[ApiController]
[Route("api/tickets")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketRepository _tickets;

    public TicketsApiController(ITicketRepository tickets)
    {
        _tickets = tickets;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == nameof(UserRole.Admin))
            return Ok(await _tickets.GetAllPagedAsync(page, pageSize, ct));

        if (role == nameof(UserRole.Agent))
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _tickets.GetForAgentPagedAsync(id, page, pageSize, ct: ct));
        }

        if (role == nameof(UserRole.User))
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _tickets.GetForUserPagedAsync(id, null, null, null, page, pageSize, ct));
        }

        return Forbid();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> Get(int id, CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(id, ct);
        if (ticket is null) return NotFound();

        var role = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ok =
            role == UserRole.Admin ||
            (role == UserRole.User && ticket.UserId == userId) ||
            (role == UserRole.Agent && ticket.AssignedAgentId == userId);

        if (!ok) return Forbid();

        return Ok(new
        {
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.ProblemType,
            ticket.CreatedAt,
            ticket.SLADeadline,
            ticket.IsOverdue,
            ticket.AssignedAgentId,
            AssignedAgentName = ticket.AssignedAgent?.Username,
            Requester = ticket.User.Username
        });
    }
}
