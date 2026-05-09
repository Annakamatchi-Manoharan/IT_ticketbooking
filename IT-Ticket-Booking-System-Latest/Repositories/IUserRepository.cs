using ITBookingSystem.Models;

namespace ITBookingSystem.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<User>> GetAgentsAsync(CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<List<User>> GetAdminUsersAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(User user, CancellationToken ct = default);
    Task<int> CountActiveTicketsForAgentAsync(int agentId, CancellationToken ct = default);

    Task<List<User>> GetAgentsForSkillAssignmentAsync(AgentSkill skill, CancellationToken ct = default);
    Task<List<User>> GetAgentsOrderedByWorkloadAsync(CancellationToken ct = default);

    Task<List<User>> GetSeniorAgentsForSkillAsync(AgentSkill skill, CancellationToken ct = default);
    Task<List<User>> GetSeniorAgentsAsync(CancellationToken ct = default);

    Task AdjustAgentActiveTicketCountAsync(int agentId, int delta, CancellationToken ct = default);

    /// <summary>Atomic SQL update — use inside transactions to avoid race conditions.</summary>
    Task AdjustAgentWorkloadSqlAsync(int agentId, int delta, CancellationToken ct = default);
}
