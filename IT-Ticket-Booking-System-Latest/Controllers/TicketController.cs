using System.Security.Claims;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using ITBookingSystem.Services;
using ITBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly TicketService _ticketService;
    private readonly IUserRepository _users;
    private readonly ITicketRepository _tickets;
    private readonly ITicketHistoryRepository _history;
    private readonly EngineerAvailabilityService _engineerAvailability;

    public TicketController(
        TicketService ticketService,
        IUserRepository users,
        ITicketRepository tickets,
        ITicketHistoryRepository history,
        EngineerAvailabilityService engineerAvailability)
    {
        _ticketService = ticketService;
        _users = users;
        _tickets = tickets;
        _history = history;
        _engineerAvailability = engineerAvailability;
    }

    [Authorize(Roles = "User,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        return View(new TicketCreateVm());
    }

    [Authorize(Roles = "User,Admin")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    [RequestSizeLimit(52_428_800)]
    [HttpPost]
    public async Task<IActionResult> Create(TicketCreateVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _ticketService.CreateAsync(userId, vm.Ticket, vm.Attachments, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }

        return RedirectToAction(nameof(MyTickets));
    }

    [Authorize(Roles = "User,Admin")]
    [HttpGet]
    public async Task<IActionResult> MyTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var paged = await _ticketService.GetMyTicketsPagedAsync(userId, status, priority, q, page, pageSize, ct);
        ViewBag.Status = status;
        ViewBag.Priority = priority;
        ViewBag.Query = q;
        return View(paged);
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpGet]
    public async Task<IActionResult> Assigned(
        [FromQuery] TicketPriority? priority,
        [FromQuery] ProblemType? problemType,
        [FromQuery] string? slaStatus,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? q,
        [FromQuery] string? focus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vm = await _ticketService.BuildAssignedDashboardAsync(userId, ct);
        vm.PagedTickets = await _ticketService.GetAssignedPagedAsync(userId, page, pageSize, priority, problemType, slaStatus, fromDate, toDate, q, ct);
        vm.Priority = priority;
        vm.ProblemType = problemType;
        vm.SlaStatus = slaStatus;
        vm.FromDate = fromDate;
        vm.ToDate = toDate;
        vm.Query = q;
        ViewBag.Focus = focus;
        return View(vm);
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpGet]
    public async Task<IActionResult> AssignedTicketDetails(int id, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ticket = await _tickets.GetByIdAsync(id, ct);
        if (ticket is null) return NotFound();
        if (ticket.AssignedAgentId != actorId && !User.IsInRole(nameof(UserRole.Admin)))
            return Forbid();

        var history = await _history.GetForTicketAsync(id, ct);
        var attachments = (ticket.AttachmentPaths ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Json(new
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
            ticket.ResolvedAt,
            RequesterName = ticket.User.Username,
            ticket.RequesterEmail,
            ticket.RequesterPhone,
            Attachments = attachments,
            Comments = ticket.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CreatedAt,
                    c.Message,
                    User = c.User.Username
                }),
            Timeline = history.Select(h => new
            {
                h.CreatedAtUtc,
                h.Details,
                Action = h.Action.ToString(),
                Actor = h.ActorUser != null ? h.ActorUser.Username : "System"
            })
        });
    }

    [Authorize(Roles = "Agent,Admin")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> EngineerUpdate(int id, [FromForm] string status, [FromForm] string? comment, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!status.Equals("resolved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only resolve action is allowed.";
            return RedirectToAction(nameof(Assigned), new { focus = "recent" });
        }

        try
        {
            await _ticketService.ResolveTicketAsync(id, actorId, comment, ct);
            TempData["Success"] = "Ticket resolved successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Assigned), new { focus = "recent" });
    }

    [Authorize(Roles = "Agent")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> SetAvailability([FromForm] string mode, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var normalized = mode.Trim().ToLowerInvariant();
        var isOnline = normalized switch
        {
            "online" => true,
            "available" => true,
            _ => false
        };

        await _engineerAvailability.ToggleEngineerStatusAsync(actorId, isOnline, ct);
        TempData["Success"] = isOnline ? "You are now online." : "You are now offline.";
        return RedirectToAction(nameof(Assigned));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetByIdAsync(id, ct);
        if (ticket is null) return NotFound();

        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

        var canSee =
            role == UserRole.Admin ||
            (role == UserRole.User && ticket.UserId == actorId) ||
            (role == UserRole.Agent && ticket.AssignedAgentId == actorId);

        if (!canSee) return Forbid();

        ViewBag.History = await _history.GetForTicketAsync(id, ct);
        return View(ticket);
    }

    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, TicketStatus status, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

        try
        {
            await _ticketService.UpdateStatusAsync(id, status, actorId, role, ct);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> AddComment(int id, [FromForm] string message, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

        if (string.IsNullOrWhiteSpace(message))
            return RedirectToAction(nameof(Details), new { id });

        try
        {
            await _ticketService.AddCommentAsync(id, actorId, role, message, ct);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, "Unable to add comment.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
