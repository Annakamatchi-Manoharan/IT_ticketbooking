using ITBookingSystem.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ITBookingSystem.Services;

public class TicketRealtimePublisher
{
    private readonly IHubContext<TicketHub> _hub;

    public TicketRealtimePublisher(IHubContext<TicketHub> hub)
    {
        _hub = hub;
    }

    public Task TicketCreatedAsync(object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("TicketCreated", payload, ct);

    public Task TicketUpdatedAsync(object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("TicketUpdated", payload, ct);
}
