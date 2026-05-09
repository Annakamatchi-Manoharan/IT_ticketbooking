using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ITBookingSystem.Hubs;

[Authorize]
public class TicketHub : Hub
{
}
