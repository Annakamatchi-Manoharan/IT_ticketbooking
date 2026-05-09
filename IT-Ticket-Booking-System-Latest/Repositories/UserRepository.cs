using ITBookingSystem.Data;
using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<List<User>> GetAgentsAsync(CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        _db.Users.OrderBy(u => u.Username).ToListAsync(ct);

    public Task<List<User>> GetAdminUsersAsync(CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountActiveTicketsForAgentAsync(int agentId, CancellationToken ct = default) =>
        _db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Escalated), ct);

    public Task<List<User>> GetAgentsForSkillAssignmentAsync(AgentSkill skill, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline && u.Skill == skill)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public Task<List<User>> GetAgentsOrderedByWorkloadAsync(CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public async Task AdjustAgentActiveTicketCountAsync(int agentId, int delta, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == agentId, ct);
        if (u is null) return;
        u.ActiveTicketCount = Math.Max(0, u.ActiveTicketCount + delta);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<User>> GetSeniorAgentsForSkillAsync(AgentSkill skill, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline && u.IsSeniorAgent && u.Skill == skill)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public Task<List<User>> GetSeniorAgentsAsync(CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline && u.IsSeniorAgent)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public Task AdjustAgentWorkloadSqlAsync(int agentId, int delta, CancellationToken ct = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Users SET ActiveTicketCount = CASE WHEN ActiveTicketCount + {delta} < 0 THEN 0 ELSE ActiveTicketCount + {delta} END WHERE Id = {agentId}",
            ct);
}
