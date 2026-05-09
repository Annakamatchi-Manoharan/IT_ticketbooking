using System.Data;
using ITBookingSystem.Data;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using ITBookingSystem.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Services;

public class TicketService
{
    private const long MaxAttachmentBytes = 10 * 1024 * 1024;
    private const int MaxActiveTicketsPerEngineer = 25;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".txt", ".log", ".zip"
    };

    private readonly AppDbContext _db;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly SLAService _sla;
    private readonly IDataProtector _tvProtector;
    private readonly IWebHostEnvironment _env;
    private readonly EnterpriseNotificationService _notify;
    private readonly TicketRealtimePublisher _realtime;
    private readonly AssignmentEngineService _assignmentEngine;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        AppDbContext db,
        ITicketRepository tickets,
        IUserRepository users,
        SLAService sla,
        IDataProtectionProvider dataProtection,
        IWebHostEnvironment env,
        EnterpriseNotificationService notify,
        TicketRealtimePublisher realtime,
        AssignmentEngineService assignmentEngine,
        ILogger<TicketService> logger)
    {
        _db = db;
        _tickets = tickets;
        _users = users;
        _sla = sla;
        _tvProtector = dataProtection.CreateProtector("ITBooking.TeamViewer.v1");
        _env = env;
        _notify = notify;
        _realtime = realtime;
        _assignmentEngine = assignmentEngine;
        _logger = logger;
    }

    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _tickets.GetByIdAsync(id, ct);

    public async Task<Ticket> CreateAsync(int userId, TicketCreateDto dto, IEnumerable<IFormFile>? attachments, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var description = InputSanitizer.SanitizePlainText(dto.Description, 4000);
        if (description.Length == 0)
            throw new InvalidOperationException("Description is required.");

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var titleRaw = string.IsNullOrWhiteSpace(dto.Title)
                    ? $"{dto.ProblemType} — {now:MMM dd, yyyy HH:mm}"
                    : dto.Title;
                var title = InputSanitizer.SanitizePlainText(titleRaw, 200);
                if (title.Length == 0)
                    title = $"{dto.ProblemType} — {now:MMM dd, yyyy HH:mm}";

                string? tvProtected = null;
                if (!string.IsNullOrWhiteSpace(dto.TeamViewerPassword))
                    tvProtected = _tvProtector.Protect(dto.TeamViewerPassword.Trim());

                var ticket = new Ticket
                {
                    Title = title,
                    Description = description,
                    ProblemType = dto.ProblemType,
                    Priority = dto.Priority,
                    Status = TicketStatus.Open,
                    CreatedAt = now,
                    SLADeadline = _sla.CalculateDeadlineUtc(dto.Priority, now),
                    IsOverdue = false,
                    UserId = userId,
                    AssignedAgentId = null,
                    WorkLocation = dto.WorkLocation,
                    TeamViewerId = string.IsNullOrWhiteSpace(dto.TeamViewerId) ? null : InputSanitizer.SanitizePlainText(dto.TeamViewerId, 64),
                    TeamViewerPasswordProtected = tvProtected,
                    RequesterPhone = string.IsNullOrWhiteSpace(dto.ContactNumber) ? null : InputSanitizer.SanitizePlainText(dto.ContactNumber, 32),
                    RequesterEmail = string.IsNullOrWhiteSpace(dto.Email) ? null : InputSanitizer.SanitizePlainText(dto.Email, 255).ToLowerInvariant(),
                    EscalationProcessed = false,
                    UpdatedAt = now,
                    QueueStatus = QueueStatus.PendingAssignment,
                    AssignedAt = null
                };

                _db.Tickets.Add(ticket);
                await _db.SaveChangesAsync(ct);

                _db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.Id,
                    Action = TicketAuditAction.Created,
                    Details = $"Ticket created (priority {ticket.Priority}, type {ticket.ProblemType}).",
                    ActorUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });

                _db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.Id,
                    Action = TicketAuditAction.Assigned,
                    Details = "Pending assignment in engineer queue.",
                    ActorUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                var savedPaths = await SaveAttachmentsAsync(ticket.Id, attachments, ct);
                if (savedPaths.Count > 0)
                {
                    ticket.AttachmentPaths = string.Join(';', savedPaths);
                    await _tickets.UpdateAsync(ticket, ct);
                }

                try
                {
                    if (ticket.Priority == TicketPriority.Critical)
                    {
                        await _assignmentEngine.AssignQueuedTicketsAsync(ct);
                    }
                    else
                    {
                        await _notify.NotifyAutoAssignmentAsync(ticket, null, ct);
                    }
                    await _realtime.TicketCreatedAsync(new { ticket.Id, ticket.Title, ticket.Status, ticket.AssignedAgentId }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Post-create notify/realtime failed for ticket {TicketId}", ticket.Id);
                }

                return ticket;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public Task<PagedResult<TicketListItemDto>> GetMyTicketsPagedAsync(int userId, TicketStatus? status, TicketPriority? priority, string? search, int page, int pageSize, CancellationToken ct = default) =>
        _tickets.GetForUserPagedAsync(userId, status, priority, search, page, pageSize, ct);

    public Task<List<Ticket>> GetMyTicketsAsync(int userId, TicketStatus? status, TicketPriority? priority, string? search, CancellationToken ct = default) =>
        _tickets.GetForUserAsync(userId, status, priority, search, ct);

    public Task<List<Ticket>> GetAssignedAsync(int agentId, CancellationToken ct = default) =>
        _tickets.GetForAgentAsync(agentId, ct);

    public Task<PagedResult<TicketListItemDto>> GetAssignedPagedAsync(
        int agentId,
        int page,
        int pageSize,
        TicketPriority? priority,
        ProblemType? problemType,
        string? slaStatus,
        DateTime? fromDate,
        DateTime? toDate,
        string? search,
        CancellationToken ct = default) =>
        _tickets.GetForAgentPagedAsync(
            agentId,
            page,
            pageSize,
            priority,
            problemType,
            slaStatus,
            fromDate,
            toDate,
            search,
            includeResolved: false,
            ct: ct);

    public Task EngineerUpdateAsync(int ticketId, int actorUserId, string? comment, string updateStatus, CancellationToken ct = default) =>
        ResolveTicketAsync(ticketId, actorUserId, comment, ct);

    public async Task ResolveTicketAsync(int ticketId, int actorUserId, string? comment, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var ticket = await _db.Tickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
                ?? throw new InvalidOperationException("Ticket not found.");

            if (ticket.AssignedAgentId != actorUserId)
                throw new UnauthorizedAccessException("You can update only your assigned tickets.");
            if (ticket.Status == TicketStatus.Resolved)
                throw new InvalidOperationException("Ticket is already resolved.");

            var now = DateTime.UtcNow;
            var cleanComment = string.IsNullOrWhiteSpace(comment) ? null : InputSanitizer.SanitizePlainText(comment, 2000);
            if (string.IsNullOrWhiteSpace(cleanComment))
                throw new InvalidOperationException("Resolution comment is required.");

            _db.Comments.Add(new Comment
            {
                TicketId = ticketId,
                UserId = actorUserId,
                Message = cleanComment,
                CreatedAt = now
            });
            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticketId,
                Action = TicketAuditAction.CommentAdded,
                Details = "Resolution comment added.",
                ActorUserId = actorUserId,
                CreatedAtUtc = now
            });

            var oldStatus = ticket.Status;
            if (ticket.AssignedAgentId is int aid)
                await _users.AdjustAgentWorkloadSqlAsync(aid, -1, ct);

            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = now;
            ticket.IsOverdue = now > ticket.SLADeadline;
            ticket.QueueStatus = QueueStatus.Resolved;
            ticket.ResolutionComment = cleanComment;

            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticketId,
                Action = TicketAuditAction.StatusChanged,
                Details = $"Status changed from {oldStatus} to {TicketStatus.Resolved}.",
                ActorUserId = actorUserId,
                CreatedAtUtc = now
            });

            ticket.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            try
            {
                await _notify.NotifyTicketActivityAsync(ticket, actorUserId, oldStatus, TicketStatus.Resolved, cleanComment, ct);
                await _realtime.TicketUpdatedAsync(new
                {
                    Id = ticket.Id,
                    Status = ticket.Status,
                    QueueStatus = ticket.QueueStatus,
                    UpdatedBy = actorUserId,
                    Event = "Resolved"
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Post update notifications failed for ticket {TicketId}", ticket.Id);
            }
        });
    }

    public async Task UpdateStatusAsync(int ticketId, TicketStatus newStatus, int actorUserId, UserRole actorRole, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
                           ?? throw new InvalidOperationException("Ticket not found.");

            var can =
                actorRole == UserRole.Admin ||
                (actorRole == UserRole.Agent && ticket.AssignedAgentId == actorUserId);

            if (!can)
                throw new UnauthorizedAccessException("Not allowed.");

            var old = ticket.Status;
            static bool CountsTowardLoad(TicketStatus s) =>
                s is TicketStatus.Open or TicketStatus.InProgress or TicketStatus.Escalated;

            if (ticket.AssignedAgentId is int aid)
            {
                if (CountsTowardLoad(old) && newStatus == TicketStatus.Resolved)
                    await _users.AdjustAgentWorkloadSqlAsync(aid, -1, ct);
                else if (old == TicketStatus.Resolved && newStatus != TicketStatus.Resolved)
                    await _users.AdjustAgentWorkloadSqlAsync(aid, 1, ct);
            }

            ticket.Status = newStatus;
            if (newStatus == TicketStatus.Resolved)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
                ticket.IsOverdue = ticket.ResolvedAt > ticket.SLADeadline;
            }
            else if (old == TicketStatus.Resolved)
                ticket.ResolvedAt = null;

            ticket.UpdatedAt = DateTime.UtcNow;

            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                Action = TicketAuditAction.StatusChanged,
                Details = $"Status changed from {old} to {newStatus}.",
                ActorUserId = actorUserId,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        try
        {
            await _realtime.TicketUpdatedAsync(new { Id = ticketId, Status = newStatus }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Realtime update failed for ticket {TicketId}", ticketId);
        }
    }

    public async Task AddCommentAsync(int ticketId, int userId, UserRole role, string message, CancellationToken ct = default)
    {
        var text = InputSanitizer.SanitizePlainText(message, 2000);
        if (text.Length == 0) throw new InvalidOperationException("Comment cannot be empty.");

        var ticket = await _tickets.GetByIdAsync(ticketId, ct) ?? throw new InvalidOperationException("Ticket not found.");

        var can =
            role == UserRole.Admin ||
            (role == UserRole.User && ticket.UserId == userId);

        if (!can)
            throw new UnauthorizedAccessException("Not allowed.");

        await _tickets.AddCommentAsync(new Comment
        {
            TicketId = ticketId,
            UserId = userId,
            Message = text,
            CreatedAt = DateTime.UtcNow
        }, ct);

        _db.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticketId,
            Action = TicketAuditAction.CommentAdded,
            Details = "Comment added.",
            ActorUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        try
        {
            await _realtime.TicketUpdatedAsync(new { Id = ticketId, Event = "CommentAdded" }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Realtime comment event failed for ticket {TicketId}", ticketId);
        }
    }

    public async Task ReassignTicketAsync(int ticketId, int? newAgentId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
                           ?? throw new InvalidOperationException("Ticket not found.");

            if (newAgentId is int na)
            {
                var agent = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == na, ct);
                if (agent is null || agent.Role != UserRole.Agent || !agent.IsActive)
                    throw new InvalidOperationException("Target agent is invalid.");
            }

            static bool CountsTowardLoad(TicketStatus s) =>
                s is TicketStatus.Open or TicketStatus.InProgress or TicketStatus.Escalated;

            var old = ticket.AssignedAgentId;
            if (old == newAgentId)
            {
                await tx.CommitAsync(ct);
                return;
            }

            if (old is int o && CountsTowardLoad(ticket.Status))
                await _users.AdjustAgentWorkloadSqlAsync(o, -1, ct);
            if (newAgentId is int n && CountsTowardLoad(ticket.Status))
                await _users.AdjustAgentWorkloadSqlAsync(n, 1, ct);

            ticket.AssignedAgentId = newAgentId;
            ticket.UpdatedAt = DateTime.UtcNow;

            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                Action = TicketAuditAction.Assigned,
                Details = newAgentId is int nid ? $"Reassigned to agent ID {nid}." : "Unassigned.",
                ActorUserId = null,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        try
        {
            if (newAgentId is int nid2)
            {
                var t = await _tickets.GetByIdAsync(ticketId, ct);
                if (t is not null)
                    await _notify.NotifyTicketReassignedAsync(t, nid2, ct);
            }

            await _realtime.TicketUpdatedAsync(new { Id = ticketId, AssignedAgentId = newAgentId }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-reassign notify failed for ticket {TicketId}", ticketId);
        }
    }

    public async Task<AssignedTicketsVm> BuildAssignedDashboardAsync(int agentId, CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var summary = await _tickets.GetAgentSummaryAsync(agentId, ct);
        var resolvedToday = await _db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId &&
            t.Status == TicketStatus.Resolved &&
            t.ResolvedAt != null &&
            t.ResolvedAt >= todayStart, ct);
        var openTickets = await _db.Tickets.CountAsync(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.Open, ct);
        var inProgressTickets = await _db.Tickets.CountAsync(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.InProgress, ct);
        var compliance = await _tickets.GetSlaCompliancePercentAsync(30, ct);
        var resolvedTickets = await _tickets.GetResolvedTicketDtosForAgentAsync(agentId, 20, ct);
        var engineer = await _db.Users.AsNoTracking()
            .Where(u => u.Id == agentId)
            .Select(u => new { u.IsOnline, u.LastSeenAt })
            .FirstOrDefaultAsync(ct);

        return new AssignedTicketsVm
        {
            ActiveTickets = summary.activeTickets,
            OpenTickets = openTickets,
            InProgressTickets = inProgressTickets,
            ResolvedToday = resolvedToday,
            SlaBreaches = summary.slaBreaches,
            AvgResolutionHours = summary.avgResolutionHours,
            SlaCompliancePercent = compliance,
            ResolvedTickets = resolvedTickets,
            IsOnline = engineer?.IsOnline ?? false,
            LastSeenAt = engineer?.LastSeenAt
        };
    }

    public Task<int> AssignQueuedTicketsAsync(CancellationToken ct = default) =>
        _assignmentEngine.AssignQueuedTicketsAsync(ct);

    public Task<int?> AssignBestEngineerAsync(ProblemType problemType, TicketPriority priority, CancellationToken ct = default) =>
        AssignNextEngineerAsync(problemType, priority, ct);

    public Task<int?> AssignLeastBusyEngineerAsync(ProblemType problemType, TicketPriority priority, CancellationToken ct = default) =>
        AssignNextEngineerAsync(problemType, priority, ct);

    public async Task<int?> AssignNextEngineerAsync(ProblemType problemType, TicketPriority priority, CancellationToken ct = default)
    {
        var skillMatched = await GetEligibleEngineersAsync(problemType, ct);
        var pool = skillMatched.Count > 0 ? skillMatched : await GetAllAvailableEngineersAsync(ct);
        if (pool.Count == 0)
        {
            _logger.LogWarning("No available engineers found for problem type {ProblemType}", problemType);
            return null;
        }

        var ordered = await BalanceWorkloadAsync(pool, ct);
        // Critical/high still get immediate assignment from same balanced pool.
        _ = priority;
        return ordered.First().Id;
    }

    public Task<List<User>> GetEligibleEngineersAsync(ProblemType problemType, CancellationToken ct = default) =>
        GetAvailableEngineersAsync(problemType, ct);

    public async Task<List<User>> GetAvailableEngineersAsync(ProblemType problemType, CancellationToken ct = default)
    {
        var requiredSkill = _sla.MapProblemToSkill(problemType);
        return await _db.Users
            .Where(u =>
                u.Role == UserRole.Agent &&
                u.IsActive &&
                u.IsOnline &&
                u.Skill == requiredSkill &&
                u.ActiveTicketCount < MaxActiveTicketsPerEngineer)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);
    }

    public Task UpdateEngineerWorkloadAsync(int engineerId, int delta, CancellationToken ct = default) =>
        _users.AdjustAgentWorkloadSqlAsync(engineerId, delta, ct);

    private Task<List<User>> GetAllAvailableEngineersAsync(CancellationToken ct = default) =>
        _db.Users
            .Where(u =>
                u.Role == UserRole.Agent &&
                u.IsActive &&
                u.IsOnline &&
                u.ActiveTicketCount < MaxActiveTicketsPerEngineer)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public async Task<List<User>> BalanceWorkloadAsync(List<User> engineers, CancellationToken ct = default)
    {
        var ids = engineers.Select(e => e.Id).ToList();
        var liveWorkloadByEngineer = await _db.Tickets.AsNoTracking()
            .Where(t =>
                t.AssignedAgentId != null &&
                ids.Contains(t.AssignedAgentId.Value) &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Escalated))
            .GroupBy(t => t.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count, ct);

        var minLoad = engineers.Min(e => liveWorkloadByEngineer.TryGetValue(e.Id, out var c) ? c : 0);
        var minLoadEngineers = engineers
            .Where(e => (liveWorkloadByEngineer.TryGetValue(e.Id, out var c) ? c : 0) == minLoad)
            .OrderBy(e => e.Id)
            .ToList();

        if (minLoadEngineers.Count > 1)
        {
            var lastAssignedEngineerId = await GetLastAssignedEngineerAsync(minLoadEngineers.Select(e => e.Id).ToList(), ct);
            if (lastAssignedEngineerId is int last)
            {
                var next = minLoadEngineers.FirstOrDefault(e => e.Id > last) ?? minLoadEngineers.First();
                return minLoadEngineers
                    .OrderBy(e => e.Id == next.Id ? 0 : 1)
                    .ThenBy(e => e.Id)
                    .ToList();
            }
        }

        var lastAssignedByEngineer = await _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId != null && ids.Contains(t.AssignedAgentId.Value))
            .GroupBy(t => t.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, LastAssignedAt = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.AgentId, x => x.LastAssignedAt, ct);

        return engineers
            .OrderBy(e => liveWorkloadByEngineer.TryGetValue(e.Id, out var c) ? c : 0)
            .ThenBy(e => lastAssignedByEngineer.TryGetValue(e.Id, out var ts) ? ts : DateTime.MinValue)
            .ThenBy(e => e.Id)
            .ToList();
    }

    public async Task<int?> GetLastAssignedEngineerAsync(List<int> candidateEngineerIds, CancellationToken ct = default)
    {
        if (candidateEngineerIds.Count == 0) return null;
        var last = await _db.Tickets.AsNoTracking()
            .Where(t => t.AssignedAgentId != null && candidateEngineerIds.Contains(t.AssignedAgentId.Value))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.AssignedAgentId)
            .FirstOrDefaultAsync(ct);
        return last;
    }

    private async Task<List<string>> SaveAttachmentsAsync(int ticketId, IEnumerable<IFormFile>? files, CancellationToken ct)
    {
        var list = new List<string>();
        if (files is null) return list;

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > MaxAttachmentBytes) continue;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                continue;

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var rel = Path.Combine("uploads", "tickets", ticketId.ToString(), safeName).Replace('\\', '/');
            var physical = Path.Combine(_env.WebRootPath, "uploads", "tickets", ticketId.ToString());
            Directory.CreateDirectory(physical);
            var full = Path.Combine(physical, safeName);

            await using (var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
                await file.CopyToAsync(stream, ct);

            list.Add("/" + rel);
        }

        return list;
    }
}
