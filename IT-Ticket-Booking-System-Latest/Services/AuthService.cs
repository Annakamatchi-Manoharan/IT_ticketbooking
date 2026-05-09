using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ITBookingSystem.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<(bool ok, string error)> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var existing = await _users.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);
        if (existing is not null)
            return (false, "Email already exists.");

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Skill = dto.Role == UserRole.Agent ? dto.Skill : null,
            IsAvailable = true,
            IsOnline = false,
            IsActive = true,
            ActiveTicketCount = 0
        };

        await _users.AddAsync(user, ct);
        return (true, string.Empty);
    }

    public async Task<(User? user, string? jwt, string error)> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
            return (null, null, "Invalid email or password.");

        if (!user.IsActive)
            return (null, null, "This account has been deactivated. Contact an administrator.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, null, "Invalid email or password.");

        var jwt = GenerateJwt(user);
        return (user, jwt, string.Empty);
    }

    public ClaimsPrincipal BuildPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.Skill is not null)
            claims.Add(new Claim("skill", user.Skill.Value.ToString()));

        var identity = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(identity);
    }

    private string GenerateJwt(User user)
    {
        var secret = _config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            secret = "DEV_ONLY_CHANGE_ME__PLEASE_SET_JWT_SECRET_32CHARS_MIN";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

