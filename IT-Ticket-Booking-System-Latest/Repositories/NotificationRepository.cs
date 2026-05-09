using ITBookingSystem.Data;
using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(InAppNotification n, CancellationToken ct = default)
    {
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<InAppNotification>> GetRecentForUserAsync(int userId, int take, CancellationToken ct = default) =>
        _db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public Task<List<InAppNotification>> GetUnreadForUserAsync(int userId, int take, CancellationToken ct = default) =>
        _db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsRead)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task MarkReadAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (n is null) return;
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);
    }
}
