using System.Data;
using ITBookingSystem.Data;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Services;

public class EscalationService
{
    private readonly AppDbContext _db;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly SLAService _sla;
    private readonly EnterpriseNotificationService _notify;
    private readonly TicketRealtimePublisher _realtime;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(
        AppDbContext db,
        ITicketRepository tickets,
        IUserRepository users,
        SLAService sla,
        EnterpriseNotificationService notify,
        TicketRealtimePublisher realtime,
        ILogger<EscalationService> logger)
    {
        _db = db;
        _tickets = tickets;
        _users = users;
        _sla = sla;
        _notify = notify;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<int> ProcessPendingEscalationsAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var pending = await _tickets.GetTicketsNeedingEscalationAsync(ct);
            var count = 0;
            foreach (var ticket in pending)
            {
                try
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == ticket.Id, ct);
                    if (t is null || t.EscalationProcessed || t.Status == TicketStatus.Resolved || !t.IsOverdue)
                    {
                        await tx.RollbackAsync(ct);
                        continue;
                    }

                    var skill = _sla.MapProblemToSkill(t.ProblemType);
                    var q = _db.Users.Where(u =>
                        u.Role == UserRole.Agent && u.IsActive && u.IsOnline && u.IsSeniorAgent && u.Skill == skill);
                    if (!await q.AnyAsync(ct))
                        q = _db.Users.Where(u => u.Role == UserRole.Agent && u.IsActive && u.IsOnline && u.IsSeniorAgent);

                    var senior = await q.OrderBy(u => u.ActiveTicketCount).ThenBy(u => u.Id).FirstOrDefaultAsync(ct);

                    var oldAgentId = t.AssignedAgentId;

                    static bool Counts(TicketStatus s) =>
                        s is TicketStatus.Open or TicketStatus.InProgress or TicketStatus.Escalated;

                    if (senior is not null && senior.Id != oldAgentId)
                    {
                        if (oldAgentId is int o && Counts(t.Status))
                            await _users.AdjustAgentWorkloadSqlAsync(o, -1, ct);
                        if (Counts(t.Status))
                            await _users.AdjustAgentWorkloadSqlAsync(senior.Id, 1, ct);

                        t.AssignedAgentId = senior.Id;
                    }

                    t.Status = TicketStatus.Escalated;
                    t.EscalationProcessed = true;
                    t.UpdatedAt = DateTime.UtcNow;

                    _db.TicketHistories.Add(new TicketHistory
                    {
                        TicketId = t.Id,
                        Action = TicketAuditAction.Escalated,
                        Details = senior is null
                            ? "SLA breached — escalated (no senior agent available; admins notified)."
                            : $"SLA breached — escalated and reassigned to senior agent {senior.Username} (ID {senior.Id}).",
                        ActorUserId = null,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    count++;

                    await _notify.NotifySlaBreachAdminsAsync(t, ct);
                    if (senior is not null)
                        await _notify.NotifyTicketReassignedAsync(t, senior.Id, ct);

                    await _realtime.TicketUpdatedAsync(new { t.Id, t.Status, t.AssignedAgentId }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Escalation failed for ticket {TicketId}", ticket.Id);
                }
            }

            return count;
        });
    }
}
