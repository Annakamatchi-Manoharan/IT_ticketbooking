using ITBookingSystem.Data;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Tickets
            .Include(t => t.User)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Ticket>> GetForUserAsync(int userId, TicketStatus? status = null, TicketPriority? priority = null, string? search = null, CancellationToken ct = default)
    {
        var q = _db.Tickets
            .AsNoTracking()
            .Include(t => t.AssignedAgent)
            .Where(t => t.UserId == userId);

        if (status is not null)
            q = q.Where(t => t.Status == status);
        if (priority is not null)
            q = q.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var clean = s.TrimStart('#').Replace("INC-", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, out var idNum))
                q = q.Where(t => t.Id == idNum || t.Title.Contains(s) || t.Description.Contains(s));
            else
                q = q.Where(t => t.Title.Contains(s) || t.Description.Contains(s));
        }

        return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public Task<List<Ticket>> GetForAgentAsync(int agentId, CancellationToken ct = default) =>
        _db.Tickets
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.AssignedAgentId == agentId && t.QueueStatus == QueueStatus.Assigned && t.Status != TicketStatus.Resolved)
            .OrderBy(t => t.Status)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Ticket>> GetAllAsync(CancellationToken ct = default) =>
        _db.Tickets
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.AssignedAgent)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountOverdueAsync(CancellationToken ct = default) =>
        _db.Tickets.CountAsync(t => t.IsOverdue, ct);

    public async Task<(int total, int open, int inProgress, int resolved, int overdue, int escalated)> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var total = await _db.Tickets.CountAsync(ct);
        var open = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Open, ct);
        var inProgress = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.InProgress, ct);
        var resolved = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved, ct);
        var overdue = await _db.Tickets.CountAsync(t => t.IsOverdue, ct);
        var escalated = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Escalated, ct);
        return (total, open, inProgress, resolved, overdue, escalated);
    }

    public async Task<int> MarkOverdueAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var toMark = await _db.Tickets
            .Where(t => !t.IsOverdue && t.Status != TicketStatus.Resolved && t.SLADeadline <= utcNow)
            .ToListAsync(ct);

        foreach (var t in toMark)
        {
            t.IsOverdue = true;
            t.UpdatedAt = utcNow;
        }

        if (toMark.Count == 0) return 0;
        await _db.SaveChangesAsync(ct);
        return toMark.Count;
    }

    public Task<int> CountResolvedSinceAsync(DateTime utcInclusiveStart, CancellationToken ct = default) =>
        _db.Tickets.CountAsync(t =>
            t.Status == TicketStatus.Resolved &&
            t.ResolvedAt != null &&
            t.ResolvedAt >= utcInclusiveStart, ct);

    public async Task<List<(DateTime dayUtc, int count)>> GetTicketsCreatedPerDayAsync(int days, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-(days - 1));

        var rows = await _db.Tickets.AsNoTracking()
            .Where(t => t.CreatedAt >= start && t.CreatedAt < today.AddDays(1))
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var map = rows.ToDictionary(x => x.Day, x => x.Count);
        var result = new List<(DateTime, int)>();
        for (var d = start; d <= today; d = d.AddDays(1))
            result.Add((d, map.TryGetValue(d, out var c) ? c : 0));

        return result;
    }

    public async Task<List<DashboardActivityDto>> GetRecentActivityAsync(int take, CancellationToken ct = default)
    {
        var assigned = await _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId != null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new DashboardActivityDto
            {
                Kind = "assigned",
                Message = $"Ticket #{t.Id} assigned to agent",
                AtUtc = t.CreatedAt
            })
            .ToListAsync(ct);

        var resolved = await _db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null)
            .OrderByDescending(t => t.ResolvedAt)
            .Take(take)
            .Select(t => new DashboardActivityDto
            {
                Kind = "resolved",
                Message = $"Ticket #{t.Id} resolved",
                AtUtc = t.ResolvedAt!.Value
            })
            .ToListAsync(ct);

        var alerts = await _db.Tickets.AsNoTracking()
            .Where(t => t.IsOverdue && t.Status != TicketStatus.Resolved)
            .OrderByDescending(t => t.SLADeadline)
            .Take(Math.Min(take, 5))
            .Select(t => new DashboardActivityDto
            {
                Kind = "alert",
                Message = $"SLA breach: ticket #{t.Id} ({t.Priority})",
                AtUtc = t.UpdatedAt
            })
            .ToListAsync(ct);

        return assigned
            .Concat(resolved)
            .Concat(alerts)
            .OrderByDescending(x => x.AtUtc)
            .Take(take)
            .ToList();
    }

    public async Task AddCommentAsync(Comment comment, CancellationToken ct = default)
    {
        _db.Comments.Add(comment);
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == comment.TicketId, ct);
        if (ticket is not null)
            ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Ticket>> GetResolvedTicketsForAgentAsync(int agentId, CancellationToken ct = default) =>
        _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.Resolved && t.ResolvedAt != null)
            .ToListAsync(ct);

    public async Task<List<EngineerResolvedTicketDto>> GetResolvedTicketDtosForAgentAsync(int agentId, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        return await _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.Resolved && t.ResolvedAt != null)
            .OrderByDescending(t => t.ResolvedAt)
            .Take(take)
            .Select(t => new EngineerResolvedTicketDto
            {
                Id = t.Id,
                RequesterName = t.User != null ? t.User.Username : "—",
                FinalComment = t.ResolutionComment ?? t.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => c.Message)
                    .FirstOrDefault() ?? "No comment",
                ResolvedAt = t.ResolvedAt!.Value,
                Priority = t.Priority,
                SlaMet = t.ResolvedAt <= t.SLADeadline
            })
            .ToListAsync(ct);
    }

    public async Task<PagedResult<TicketListItemDto>> GetForUserPagedAsync(int userId, TicketStatus? status, TicketPriority? priority, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Tickets.AsNoTracking().Where(t => t.UserId == userId);

        if (status is not null)
            q = q.Where(t => t.Status == status);
        if (priority is not null)
            q = q.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var clean = s.TrimStart('#').Replace("INC-", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, out var idNum))
                q = q.Where(t => t.Id == idNum || t.Title.Contains(s) || t.Description.Contains(s));
            else
                q = q.Where(t => t.Title.Contains(s) || t.Description.Contains(s));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                ProblemType = t.ProblemType,
                CreatedAt = t.CreatedAt,
                SLADeadline = t.SLADeadline,
                IsOverdue = t.IsOverdue,
                Description = t.Description,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.Username : null,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<TicketListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<PagedResult<TicketListItemDto>> GetForAgentPagedAsync(
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
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Tickets.AsNoTracking().Where(t => t.AssignedAgentId == agentId);
        if (!includeResolved)
            q = q.Where(t => t.QueueStatus == QueueStatus.Assigned && t.Status != TicketStatus.Resolved);
        if (priority is not null)
            q = q.Where(t => t.Priority == priority.Value);
        if (problemType is not null)
            q = q.Where(t => t.ProblemType == problemType.Value);
        if (fromDate is not null)
            q = q.Where(t => t.CreatedAt >= fromDate.Value.Date);
        if (toDate is not null)
            q = q.Where(t => t.CreatedAt < toDate.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var clean = s.TrimStart('#').Replace("INC-", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, out var idNum))
                q = q.Where(t => t.Id == idNum || t.Title.Contains(s) || t.Description.Contains(s));
            else
                q = q.Where(t => t.Title.Contains(s) || t.Description.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(slaStatus))
        {
            var key = slaStatus.Trim().ToLowerInvariant();
            var warnAt = DateTime.UtcNow.AddHours(4);
            q = key switch
            {
                "overdue" => q.Where(t => t.IsOverdue || t.SLADeadline < DateTime.UtcNow),
                "warning" => q.Where(t => !t.IsOverdue && t.SLADeadline >= DateTime.UtcNow && t.SLADeadline <= warnAt),
                "safe" => q.Where(t => !t.IsOverdue && t.SLADeadline > warnAt),
                _ => q
            };
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.IsOverdue)
            .ThenBy(t => t.SLADeadline)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                ProblemType = t.ProblemType,
                CreatedAt = t.CreatedAt,
                SLADeadline = t.SLADeadline,
                IsOverdue = t.IsOverdue,
                Description = t.Description,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.Username : null,
                RequesterName = t.User != null ? t.User.Username : null,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<TicketListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public Task<List<Ticket>> GetTicketsNeedingEscalationAsync(CancellationToken ct = default) =>
        _db.Tickets
            .Where(t => t.IsOverdue && t.Status != TicketStatus.Resolved && !t.EscalationProcessed)
            .Include(t => t.AssignedAgent)
            .Include(t => t.User)
            .ToListAsync(ct);

    public async Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(int days, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow.Date.AddDays(-days);
        return await (
            from t in _db.Tickets.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.AssignedAgentId equals u.Id
            where t.Status == TicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt >= start
            group t by new { u.Id, u.Username } into g
            select new AgentPerformanceDto
            {
                AgentId = g.Key.Id,
                AgentName = g.Key.Username,
                ResolvedCount = g.Count(),
                AvgResolutionHours = (g.Average(x => (double?)EF.Functions.DateDiffSecond(x.CreatedAt, x.ResolvedAt!.Value)) ?? 0) / 3600.0
            }).OrderByDescending(x => x.ResolvedCount).ToListAsync(ct);
    }

    public async Task<List<(DateTime dayUtc, double avgResolutionHours)>> GetAvgResolutionHoursByDayAsync(int days, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-(days - 1));
        var rows = await _db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt >= start)
            .GroupBy(t => t.ResolvedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                Avg = (g.Average(x => (double?)EF.Functions.DateDiffSecond(x.CreatedAt, x.ResolvedAt!.Value)) ?? 0) / 3600.0
            })
            .ToListAsync(ct);

        var map = rows.ToDictionary(x => x.Day, x => x.Avg);
        var result = new List<(DateTime, double)>();
        for (var d = start; d <= today; d = d.AddDays(1))
            result.Add((d, map.TryGetValue(d, out var v) ? v : 0));
        return result;
    }

    public async Task<double> GetSlaCompliancePercentAsync(int days, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow.Date.AddDays(-days);
        var resolved = await _db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt >= start)
            .Select(t => new { t.ResolvedAt, t.SLADeadline })
            .ToListAsync(ct);

        if (resolved.Count == 0) return 100;
        var met = resolved.Count(x => x.ResolvedAt is { } ra && ra <= x.SLADeadline);
        return 100.0 * met / resolved.Count;
    }

    public async Task<PagedResult<TicketListItemDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Tickets.AsNoTracking();
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                ProblemType = t.ProblemType,
                CreatedAt = t.CreatedAt,
                SLADeadline = t.SLADeadline,
                IsOverdue = t.IsOverdue,
                Description = t.Description,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.Username : null,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<TicketListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<(int activeTickets, int pendingResponses, int slaBreaches, double? avgResolutionHours)> GetAgentSummaryAsync(int agentId, CancellationToken ct = default)
    {
        var activeTickets = await _db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Escalated), ct);

        var pendingResponses = await _db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId && t.Status == TicketStatus.Open, ct);

        var slaBreaches = await _db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId && t.IsOverdue && t.Status != TicketStatus.Resolved, ct);

        var resolved = await _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.Resolved && t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, ResolvedAt = t.ResolvedAt!.Value })
            .ToListAsync(ct);

        double? avgH = null;
        if (resolved.Count > 0)
            avgH = resolved.Average(x => (x.ResolvedAt - x.CreatedAt).TotalHours);

        return (activeTickets, pendingResponses, slaBreaches, avgH);
    }
}
