using System.Data;
using ITBookingSystem.Data;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Services;

public class AssignmentEngineService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _users;
    private readonly EnterpriseNotificationService _notify;
    private readonly TicketRealtimePublisher _realtime;
    private readonly ILogger<AssignmentEngineService> _logger;

    public AssignmentEngineService(
        AppDbContext db,
        IUserRepository users,
        EnterpriseNotificationService notify,
        TicketRealtimePublisher realtime,
        ILogger<AssignmentEngineService> logger)
    {
        _db = db;
        _users = users;
        _notify = notify;
        _realtime = realtime;
        _logger = logger;
    }

    public Task<List<User>> GetOnlineEngineersAsync(CancellationToken ct = default) =>
        _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline)
            .OrderBy(u => u.ActiveTicketCount)
            .ThenBy(u => u.LastSeenAt ?? DateTime.MinValue)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

    public async Task<int> AssignQueuedTicketsAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var online = await GetOnlineEngineersAsync(ct);
            if (online.Count == 0)
            {
                await tx.CommitAsync(ct);
                return 0;
            }

            var queue = await _db.Tickets
                .Where(t => t.QueueStatus == QueueStatus.PendingAssignment && t.AssignedAgentId == null)
                .OrderByDescending(t => t.Priority == TicketPriority.Critical)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(ct);

            if (queue.Count == 0)
            {
                await tx.CommitAsync(ct);
                return 0;
            }

            var loads = online.ToDictionary(x => x.Id, x => x.ActiveTicketCount);
            var lastAssignedId = await _db.Tickets.AsNoTracking()
                .Where(t => t.AssignedAgentId != null)
                .OrderByDescending(t => t.AssignedAt ?? t.CreatedAt)
                .Select(t => t.AssignedAgentId)
                .FirstOrDefaultAsync(ct);

            var assignedCount = 0;
            foreach (var ticket in queue)
            {
                var selected = SelectEngineer(online, loads, lastAssignedId);
                if (selected is null)
                    break;

                ticket.AssignedAgentId = selected.Id;
                ticket.AssignedAt = DateTime.UtcNow;
                ticket.Status = TicketStatus.InProgress;
                ticket.QueueStatus = QueueStatus.Assigned;
                ticket.UpdatedAt = DateTime.UtcNow;

                loads[selected.Id] = loads[selected.Id] + 1;
                lastAssignedId = selected.Id;
                assignedCount++;

                _db.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.Id,
                    Action = TicketAuditAction.Assigned,
                    Details = $"Assigned from queue to engineer ID {selected.Id}.",
                    ActorUserId = null,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (assignedCount > 0)
            {
                foreach (var load in loads)
                {
                    var engineer = await _db.Users.FirstOrDefaultAsync(u => u.Id == load.Key, ct);
                    if (engineer is not null)
                        engineer.ActiveTicketCount = Math.Max(0, load.Value);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                var assignedTickets = queue.Where(t => t.AssignedAgentId != null).ToList();
                foreach (var ticket in assignedTickets)
                {
                    try
                    {
                        await _notify.NotifyAutoAssignmentAsync(ticket, ticket.AssignedAgentId, ct);
                        await _realtime.TicketUpdatedAsync(new
                        {
                            ticket.Id,
                            ticket.Status,
                            ticket.QueueStatus,
                            ticket.AssignedAgentId,
                            Event = "AutoAssignedFromQueue"
                        }, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Post-assignment notify failed for ticket {TicketId}", ticket.Id);
                    }
                }
            }
            else
            {
                await tx.CommitAsync(ct);
            }

            return assignedCount;
        });
    }

    private static User? SelectEngineer(IReadOnlyList<User> online, IReadOnlyDictionary<int, int> loads, int? lastAssignedId)
    {
        if (online.Count == 0) return null;
        var minLoad = online.Min(e => loads.TryGetValue(e.Id, out var c) ? c : 0);
        var candidates = online
            .Where(e => (loads.TryGetValue(e.Id, out var c) ? c : 0) == minLoad)
            .OrderBy(e => e.Id)
            .ToList();

        if (candidates.Count == 1)
            return candidates[0];

        if (lastAssignedId is int last)
            return candidates.FirstOrDefault(e => e.Id > last) ?? candidates[0];

        return candidates[0];
    }
}
