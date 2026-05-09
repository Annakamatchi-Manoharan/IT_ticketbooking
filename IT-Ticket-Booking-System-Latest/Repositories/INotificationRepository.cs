using ITBookingSystem.Models;

namespace ITBookingSystem.Repositories;

public interface INotificationRepository
{
    Task AddAsync(InAppNotification n, CancellationToken ct = default);
    Task<List<InAppNotification>> GetRecentForUserAsync(int userId, int take, CancellationToken ct = default);
    Task<List<InAppNotification>> GetUnreadForUserAsync(int userId, int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
    Task MarkReadAsync(int notificationId, int userId, CancellationToken ct = default);
    Task MarkAllReadAsync(int userId, CancellationToken ct = default);
}
