using System.Text;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using ITBookingSystem.Services;
using ITBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserRepository _users;
    private readonly ITicketRepository _tickets;
    private readonly TicketService _ticketService;

    public AdminController(IUserRepository users, ITicketRepository tickets, TicketService ticketService)
    {
        _users = users;
        _tickets = tickets;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> AllTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] string? q,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var items = await _tickets.GetAllAsync(ct);
        var query = items.AsEnumerable();
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);
        if (priority is not null)
            query = query.Where(t => t.Priority == priority.Value);
        if (fromDate is not null)
            query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
        if (toDate is not null)
            query = query.Where(t => t.CreatedAt < toDate.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            var clean = s.TrimStart('#').Replace("INC-", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, out var idNum))
                query = query.Where(t => t.Id == idNum || t.Title.Contains(s, StringComparison.OrdinalIgnoreCase) || t.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
            else
                query = query.Where(t => t.Title.Contains(s, StringComparison.OrdinalIgnoreCase) || t.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.Status = status;
        ViewBag.Priority = priority;
        ViewBag.Query = q;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        return View(query.OrderByDescending(t => t.CreatedAt).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Users(int? edit, CancellationToken ct)
    {
        ViewBag.Users = await _users.GetAllAsync(ct);
        if (edit is int eid)
        {
            var u = await _users.GetByIdAsync(eid, ct);
            if (u is not null)
            {
                return View(new UserEditVm
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    Skill = u.Skill,
                    IsAvailable = u.IsAvailable,
                    IsActive = u.IsActive,
                    Department = u.Department,
                    ContactNumber = u.ContactNumber,
                    IsSeniorAgent = u.IsSeniorAgent
                });
            }
        }

        return View(new UserEditVm { Role = UserRole.User, IsAvailable = true, IsActive = true });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> UpsertUser(UserEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _users.GetAllAsync(ct);
            return View("Users", vm);
        }

        if (vm.Id is null)
        {
            var user = new User
            {
                Username = vm.Username.Trim(),
                Email = vm.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(vm.Password) ? "ChangeMe123!" : vm.Password),
                Role = vm.Role,
                Skill = vm.Role == UserRole.Agent ? vm.Skill : null,
                IsAvailable = vm.IsAvailable,
                IsActive = vm.IsActive,
                Department = string.IsNullOrWhiteSpace(vm.Department) ? null : vm.Department.Trim(),
                ContactNumber = string.IsNullOrWhiteSpace(vm.ContactNumber) ? null : vm.ContactNumber.Trim(),
                ActiveTicketCount = 0,
                IsSeniorAgent = vm.Role == UserRole.Agent && vm.IsSeniorAgent
            };
            await _users.AddAsync(user, ct);
        }
        else
        {
            var user = await _users.GetByIdAsync(vm.Id.Value, ct);
            if (user is null) return NotFound();

            user.Username = vm.Username.Trim();
            user.Email = vm.Email.Trim().ToLowerInvariant();
            user.Role = vm.Role;
            user.Skill = vm.Role == UserRole.Agent ? vm.Skill : null;
            user.IsAvailable = vm.IsAvailable;
            user.IsActive = vm.IsActive;
            user.Department = string.IsNullOrWhiteSpace(vm.Department) ? null : vm.Department.Trim();
            user.ContactNumber = string.IsNullOrWhiteSpace(vm.ContactNumber) ? null : vm.ContactNumber.Trim();
            user.IsSeniorAgent = vm.Role == UserRole.Agent && vm.IsSeniorAgent;
            if (vm.Role != UserRole.Agent)
                user.IsSeniorAgent = false;
            if (!string.IsNullOrWhiteSpace(vm.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);

            await _users.UpdateAsync(user, ct);
        }

        return RedirectToAction(nameof(Users));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return NotFound();
        await _users.DeleteAsync(user, ct);
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> ReassignTickets(CancellationToken ct)
    {
        var agents = await _users.GetAgentsAsync(ct);
        var all = await _tickets.GetAllAsync(ct);
        ViewBag.Agents = agents;
        return View(all);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Reassign(int ticketId, int? agentId, CancellationToken ct)
    {
        try
        {
            await _ticketService.ReassignTicketAsync(ticketId, agentId, ct);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(ReassignTickets));
    }

    [HttpGet]
    public async Task<IActionResult> ExportTicketLog(CancellationToken ct)
    {
        var tickets = await _tickets.GetAllAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Status,Priority,ProblemType,CreatedAtUtc,SLADeadlineUtc,IsOverdue,RequesterEmail,AssignedAgentId");
        foreach (var t in tickets)
        {
            sb.AppendLine(string.Join(',',
                t.Id,
                Escape(t.Title),
                t.Status,
                t.Priority,
                t.ProblemType,
                t.CreatedAt.ToString("O"),
                t.SLADeadline.ToString("O"),
                t.IsOverdue,
                Escape(t.RequesterEmail ?? ""),
                t.AssignedAgentId?.ToString() ?? ""));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"ticket-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string Escape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        var v = s.Replace("\"", "\"\"");
        return $"\"{v}\"";
    }
}
