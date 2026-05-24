using System.Security.Claims;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Tests;

internal static class TestHelpers
{
    internal static void SetUser(Controller controller, int userId, UserRole role, bool authenticated = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };
        var identity = authenticated ? new ClaimsIdentity(claims, "test") : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    internal static void SetUser(ControllerBase controller, int userId, UserRole role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        ], "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}

internal sealed class StubUserRepository : IUserRepository
{
    internal Func<CancellationToken, Task<List<User>>> OnGetAllAsync { get; set; } = _ => Task.FromResult(new List<User>());
    internal Func<int, CancellationToken, Task<User?>> OnGetByIdAsync { get; set; } = (_, _) => Task.FromResult<User?>(null);
    internal Func<string, CancellationToken, Task<User?>> OnGetByEmailAsync { get; set; } = (_, _) => Task.FromResult<User?>(null);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) => OnGetByIdAsync(id, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => OnGetByEmailAsync(email, ct);
    public Task<List<User>> GetAgentsAsync(CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task<List<User>> GetAllAsync(CancellationToken ct = default) => OnGetAllAsync(ct);
    public Task<List<User>> GetAdminUsersAsync(CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> CountActiveTicketsForAgentAsync(int agentId, CancellationToken ct = default) => Task.FromResult(0);
    public Task<List<User>> GetAgentsForSkillAssignmentAsync(AgentSkill skill, CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task<List<User>> GetAgentsOrderedByWorkloadAsync(CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task<List<User>> GetSeniorAgentsForSkillAsync(AgentSkill skill, CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task<List<User>> GetSeniorAgentsAsync(CancellationToken ct = default) => Task.FromResult(new List<User>());
    public Task AdjustAgentActiveTicketCountAsync(int agentId, int delta, CancellationToken ct = default) => Task.CompletedTask;
    public Task AdjustAgentWorkloadSqlAsync(int agentId, int delta, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class StubTicketRepository : ITicketRepository
{
    internal Func<CancellationToken, Task<List<Ticket>>> OnGetAllAsync { get; set; } = _ => Task.FromResult(new List<Ticket>());
    internal Func<int, CancellationToken, Task<Ticket?>> OnGetByIdAsync { get; set; } = (_, _) => Task.FromResult<Ticket?>(null);
    internal Func<int, int, CancellationToken, Task<PagedResult<TicketListItemDto>>> OnGetAllPagedAsync { get; set; } =
        (_, _, _) => Task.FromResult(new PagedResult<TicketListItemDto>());

    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default) => OnGetByIdAsync(id, ct);
    public Task<List<Ticket>> GetForUserAsync(int userId, TicketStatus? status = null, TicketPriority? priority = null, string? search = null, CancellationToken ct = default) => Task.FromResult(new List<Ticket>());
    public Task<List<Ticket>> GetForAgentAsync(int agentId, CancellationToken ct = default) => Task.FromResult(new List<Ticket>());
    public Task<List<Ticket>> GetAllAsync(CancellationToken ct = default) => OnGetAllAsync(ct);
    public Task AddAsync(Ticket ticket, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(Ticket ticket, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> CountOverdueAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<(int total, int open, int inProgress, int resolved, int overdue, int escalated)> GetDashboardStatsAsync(CancellationToken ct = default) => Task.FromResult((1, 1, 0, 0, 0, 0));
    public Task<int> MarkOverdueAsync(DateTime utcNow, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> CountResolvedSinceAsync(DateTime utcInclusiveStart, CancellationToken ct = default) => Task.FromResult(0);
    public Task<List<(DateTime dayUtc, int count)>> GetTicketsCreatedPerDayAsync(int days, CancellationToken ct = default) => Task.FromResult(new List<(DateTime dayUtc, int count)>());
    public Task<List<DashboardActivityDto>> GetRecentActivityAsync(int take, CancellationToken ct = default) => Task.FromResult(new List<DashboardActivityDto>());
    public Task AddCommentAsync(Comment comment, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<Ticket>> GetResolvedTicketsForAgentAsync(int agentId, CancellationToken ct = default) => Task.FromResult(new List<Ticket>());
    public Task<List<EngineerResolvedTicketDto>> GetResolvedTicketDtosForAgentAsync(int agentId, int take, CancellationToken ct = default) => Task.FromResult(new List<EngineerResolvedTicketDto>());
    public Task<PagedResult<TicketListItemDto>> GetForUserPagedAsync(int userId, TicketStatus? status, TicketPriority? priority, string? search, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new PagedResult<TicketListItemDto>());
    public Task<PagedResult<TicketListItemDto>> GetForAgentPagedAsync(int agentId, int page, int pageSize, TicketPriority? priority = null, ProblemType? problemType = null, string? slaStatus = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null, bool includeResolved = false, CancellationToken ct = default) => Task.FromResult(new PagedResult<TicketListItemDto>());
    public Task<List<Ticket>> GetTicketsNeedingEscalationAsync(CancellationToken ct = default) => Task.FromResult(new List<Ticket>());
    public Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(int days, CancellationToken ct = default) => Task.FromResult(new List<AgentPerformanceDto>());
    public Task<List<(DateTime dayUtc, double avgResolutionHours)>> GetAvgResolutionHoursByDayAsync(int days, CancellationToken ct = default) => Task.FromResult(new List<(DateTime dayUtc, double avgResolutionHours)>());
    public Task<double> GetSlaCompliancePercentAsync(int days, CancellationToken ct = default) => Task.FromResult(100d);
    public Task<PagedResult<TicketListItemDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default) => OnGetAllPagedAsync(page, pageSize, ct);
    public Task<(int activeTickets, int pendingResponses, int slaBreaches, double? avgResolutionHours)> GetAgentSummaryAsync(int agentId, CancellationToken ct = default) => Task.FromResult((0, 0, 0, (double?)null));
}

internal sealed class StubNotificationRepository : INotificationRepository
{
    internal Func<int, CancellationToken, Task<int>> OnCountUnreadAsync { get; set; } = (_, _) => Task.FromResult(0);
    internal Func<int, int, CancellationToken, Task<List<InAppNotification>>> OnGetRecentAsync { get; set; } = (_, _, _) => Task.FromResult(new List<InAppNotification>());

    public Task AddAsync(InAppNotification n, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<InAppNotification>> GetRecentForUserAsync(int userId, int take, CancellationToken ct = default) => OnGetRecentAsync(userId, take, ct);
    public Task<List<InAppNotification>> GetUnreadForUserAsync(int userId, int take, CancellationToken ct = default) => Task.FromResult(new List<InAppNotification>());
    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) => OnCountUnreadAsync(userId, ct);
    public Task MarkReadAsync(int notificationId, int userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkAllReadAsync(int userId, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class StubTicketHistoryRepository : ITicketHistoryRepository
{
    public Task<List<TicketHistory>> GetForTicketAsync(int ticketId, CancellationToken ct = default) => Task.FromResult(new List<TicketHistory>());
}
