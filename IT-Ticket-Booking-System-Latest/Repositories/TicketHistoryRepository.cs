using ITBookingSystem.Data;
using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Repositories;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly AppDbContext _db;

    public TicketHistoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<TicketHistory>> GetForTicketAsync(int ticketId, CancellationToken ct = default) =>
        _db.TicketHistories.AsNoTracking()
            .Include(h => h.ActorUser)
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .ToListAsync(ct);
}
