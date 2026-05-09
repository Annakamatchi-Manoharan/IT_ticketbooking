using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserRole.Admin))]
public class UsersApiController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersApiController(IUserRepository users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApiUserDto>>> List(CancellationToken ct = default)
    {
        var all = await _users.GetAllAsync(ct);
        return Ok(all.Select(u => new ApiUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive
        }).ToList());
    }
}
