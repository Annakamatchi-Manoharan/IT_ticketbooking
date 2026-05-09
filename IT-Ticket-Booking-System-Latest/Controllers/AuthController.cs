using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _auth;
    private readonly EngineerAvailabilityService _engineerAvailability;

    public AuthController(AuthService auth, EngineerAvailabilityService engineerAvailability)
    {
        _auth = auth;
        _engineerAvailability = engineerAvailability;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectForRole();
        return View(new LoginDto());
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectForRole();
        return View(new RegisterDto());
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        // Public sign-up only creates end users.
        dto.Role = UserRole.User;
        dto.Skill = null;

        var (ok, error) = await _auth.RegisterAsync(dto, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error);
            return View(dto);
        }

        TempData["Success"] = "Account created successfully. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var (user, jwt, error) = await _auth.LoginAsync(dto, ct);
        if (user is null || jwt is null)
        {
            ModelState.AddModelError(string.Empty, error);
            return View(dto);
        }

        HttpContext.Session.SetString("jwt", jwt);

        var principal = _auth.BuildPrincipal(user);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = dto.RememberMe });

        if (user.Role == UserRole.Agent)
            await _engineerAvailability.ToggleEngineerStatusAsync(user.Id, true, ct);

        return user.Role switch
        {
            UserRole.User => RedirectToAction("MyTickets", "Ticket"),
            UserRole.Agent => RedirectToAction("Assigned", "Ticket"),
            _ => RedirectToAction("Index", "Dashboard")
        };
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (User.IsInRole(nameof(UserRole.Agent)))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var engineerId))
                await _engineerAvailability.ToggleEngineerStatusAsync(engineerId, false);
        }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Denied() => View();

    private IActionResult RedirectForRole()
    {
        if (User.IsInRole(nameof(UserRole.User)))
            return RedirectToAction("MyTickets", "Ticket");
        if (User.IsInRole(nameof(UserRole.Agent)))
            return RedirectToAction("Assigned", "Ticket");
        return RedirectToAction("Index", "Dashboard");
    }
}
