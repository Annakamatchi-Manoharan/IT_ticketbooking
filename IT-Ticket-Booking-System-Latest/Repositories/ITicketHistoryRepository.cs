using ITBookingSystem.Models;

namespace ITBookingSystem.Repositories;

public interface ITicketHistoryRepository
{
    Task<List<TicketHistory>> GetForTicketAsync(int ticketId, CancellationToken ct = default);
}
