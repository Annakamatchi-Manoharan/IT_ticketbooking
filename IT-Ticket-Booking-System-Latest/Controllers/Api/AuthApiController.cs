using ITBookingSystem.DTOs;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthApiController(AuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<object>> Token([FromBody] LoginDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (user, jwt, error) = await _auth.LoginAsync(dto, ct);
        if (user is null || jwt is null)
            return Unauthorized(new { error });

        return Ok(new { token = jwt, user = new { user.Id, user.Username, user.Email, role = user.Role.ToString() } });
    }
}
