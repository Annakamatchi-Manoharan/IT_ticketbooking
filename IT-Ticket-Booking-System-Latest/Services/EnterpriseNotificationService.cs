using ITBookingSystem.Models;
using ITBookingSystem.Repositories;

namespace ITBookingSystem.Services;

public class EnterpriseNotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IEmailSender _email;
    private readonly ILogger<EnterpriseNotificationService> _logger;

    public EnterpriseNotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IEmailSender email,
        ILogger<EnterpriseNotificationService> logger)
    {
        _notifications = notifications;
        _users = users;
        _email = email;
        _logger = logger;
    }

    public async Task NotifyTicketCreatedAsync(Ticket ticket, int? agentId, CancellationToken ct = default)
    {
        if (agentId is not int aid) return;
        var agent = await _users.GetByIdAsync(aid, ct);
        if (agent is null) return;

        var title = "New ticket assigned";
        var msg = $"Ticket #{ticket.Id}: {ticket.Title}";
        await _notifications.AddAsync(new InAppNotification
        {
            UserId = aid,
            Title = title,
            Message = msg,
            LinkUrl = $"/Ticket/Details/{ticket.Id}",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        try
        {
            await _email.SendAsync(agent.Email, title, $"<p>{System.Net.WebUtility.HtmlEncode(msg)}</p><p><a href=\"/Ticket/Details/{ticket.Id}\">Open ticket</a></p>", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email notify create failed for ticket {TicketId}", ticket.Id);
        }
    }

    public async Task NotifyTicketReassignedAsync(Ticket ticket, int newAgentId, CancellationToken ct = default)
    {
        var agent = await _users.GetByIdAsync(newAgentId, ct);
        if (agent is null) return;

        var title = "Ticket assigned to you";
        var msg = $"Ticket #{ticket.Id}: {ticket.Title}";
        await _notifications.AddAsync(new InAppNotification
        {
            UserId = newAgentId,
            Title = title,
            Message = msg,
            LinkUrl = $"/Ticket/Details/{ticket.Id}",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        try
        {
            await _email.SendAsync(agent.Email, title, $"<p>{System.Net.WebUtility.HtmlEncode(msg)}</p>", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email notify assign failed for ticket {TicketId}", ticket.Id);
        }
    }

    public async Task NotifySlaBreachAdminsAsync(Ticket ticket, CancellationToken ct = default)
    {
        var admins = await _users.GetAdminUsersAsync(ct);
        var title = "SLA breach / escalation";
        var msg = $"Ticket #{ticket.Id} breached SLA and was escalated: {ticket.Title}";

        foreach (var a in admins)
        {
            await _notifications.AddAsync(new InAppNotification
            {
                UserId = a.Id,
                Title = title,
                Message = msg,
                LinkUrl = $"/Ticket/Details/{ticket.Id}",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            try
            {
                await _email.SendAsync(a.Email, title, $"<p>{System.Net.WebUtility.HtmlEncode(msg)}</p>", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email SLA notify failed for admin {AdminId}", a.Id);
            }
        }
    }

    public async Task NotifyTicketActivityAsync(
        Ticket ticket,
        int actorUserId,
        TicketStatus oldStatus,
        TicketStatus newStatus,
        string? comment,
        CancellationToken ct = default)
    {
        var actor = await _users.GetByIdAsync(actorUserId, ct);
        var actorName = actor?.Username ?? "Engineer";
        var statusChanged = oldStatus != newStatus;
        var commentAdded = !string.IsNullOrWhiteSpace(comment);

        var recipients = new HashSet<int>();
        recipients.Add(ticket.UserId);
        var admins = await _users.GetAdminUsersAsync(ct);
        foreach (var admin in admins)
            recipients.Add(admin.Id);
        recipients.Remove(actorUserId);

        var title = statusChanged ? "Ticket updated" : "New engineer response";
        var statusText = statusChanged ? $"{oldStatus} -> {newStatus}" : "comment updated";
        var body = $"{actorName} updated ticket #{ticket.Id} ({statusText}).";
        if (newStatus == TicketStatus.Resolved)
            body = $"{actorName} resolved ticket #{ticket.Id}.";
        if (commentAdded)
            body += " New engineer response added.";

        foreach (var userId in recipients)
        {
            await _notifications.AddAsync(new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = body,
                LinkUrl = $"/Ticket/Details/{ticket.Id}",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
        }
    }

    public async Task NotifyAutoAssignmentAsync(Ticket ticket, int? assignedAgentId, CancellationToken ct = default)
    {
        var recipients = new HashSet<int> { ticket.UserId };
        var admins = await _users.GetAdminUsersAsync(ct);
        foreach (var admin in admins)
            recipients.Add(admin.Id);
        if (assignedAgentId is int aid)
            recipients.Add(aid);

        foreach (var userId in recipients)
        {
            string title;
            string message;
            if (assignedAgentId is int && userId == assignedAgentId)
            {
                title = "New ticket assigned to you";
                message = $"Ticket #{ticket.Id} was assigned automatically.";
            }
            else if (userId == ticket.UserId)
            {
                title = assignedAgentId is int ? "Your ticket has been assigned" : "Ticket pending assignment";
                message = assignedAgentId is int
                    ? $"Ticket #{ticket.Id} was assigned to an engineer."
                    : $"Ticket #{ticket.Id} is waiting for available engineers.";
            }
            else
            {
                title = assignedAgentId is int ? "Ticket assigned automatically" : "Ticket pending assignment";
                message = assignedAgentId is int
                    ? $"Ticket #{ticket.Id} was assigned by smart assignment."
                    : $"Ticket #{ticket.Id} could not be assigned automatically.";
            }

            await _notifications.AddAsync(new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                LinkUrl = $"/Ticket/Details/{ticket.Id}",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
        }
    }
}
