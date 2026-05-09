using ITBookingSystem.DTOs;
using ITBookingSystem.Models;

namespace ITBookingSystem.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Ticket>> GetForUserAsync(int userId, TicketStatus? status = null, TicketPriority? priority = null, string? search = null, CancellationToken ct = default);
    Task<List<Ticket>> GetForAgentAsync(int agentId, CancellationToken ct = default);
    Task<List<Ticket>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
    Task<int> CountOverdueAsync(CancellationToken ct = default);

    Task<(int total, int open, int inProgress, int resolved, int overdue, int escalated)> GetDashboardStatsAsync(CancellationToken ct = default);

    Task<int> MarkOverdueAsync(DateTime utcNow, CancellationToken ct = default);

    Task<int> CountResolvedSinceAsync(DateTime utcInclusiveStart, CancellationToken ct = default);

    Task<List<(DateTime dayUtc, int count)>> GetTicketsCreatedPerDayAsync(int days, CancellationToken ct = default);

    Task<List<DashboardActivityDto>> GetRecentActivityAsync(int take, CancellationToken ct = default);

    Task AddCommentAsync(Comment comment, CancellationToken ct = default);

    Task<List<Ticket>> GetResolvedTicketsForAgentAsync(int agentId, CancellationToken ct = default);
    Task<List<EngineerResolvedTicketDto>> GetResolvedTicketDtosForAgentAsync(int agentId, int take, CancellationToken ct = default);

    Task<PagedResult<TicketListItemDto>> GetForUserPagedAsync(int userId, TicketStatus? status, TicketPriority? priority, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<TicketListItemDto>> GetForAgentPagedAsync(
        int agentId,
        int page,
        int pageSize,
        TicketPriority? priority = null,
        ProblemType? problemType = null,
        string? slaStatus = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null,
        bool includeResolved = false,
        CancellationToken ct = default);

    Task<List<Ticket>> GetTicketsNeedingEscalationAsync(CancellationToken ct = default);

    Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(int days, CancellationToken ct = default);

    Task<List<(DateTime dayUtc, double avgResolutionHours)>> GetAvgResolutionHoursByDayAsync(int days, CancellationToken ct = default);

    Task<double> GetSlaCompliancePercentAsync(int days, CancellationToken ct = default);

    Task<PagedResult<TicketListItemDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<(int activeTickets, int pendingResponses, int slaBreaches, double? avgResolutionHours)> GetAgentSummaryAsync(int agentId, CancellationToken ct = default);
}
