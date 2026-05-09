using ITBookingSystem.Models;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (User.IsInRole(nameof(UserRole.Admin)))
        {
            var vm = await _dashboard.BuildAdminDashboardAsync(ct);
            return View(vm);
        }

        if (User.IsInRole(nameof(UserRole.Agent)))
            return RedirectToAction("Assigned", "Ticket");

        return RedirectToAction("MyTickets", "Ticket");
    }
}
