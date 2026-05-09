using ITBookingSystem.Models;
using ITBookingSystem.Repositories;

namespace ITBookingSystem.Services;

public class EngineerAvailabilityService
{
    private readonly IUserRepository _users;
    private readonly AssignmentEngineService _assignmentEngine;

    public EngineerAvailabilityService(IUserRepository users, AssignmentEngineService assignmentEngine)
    {
        _users = users;
        _assignmentEngine = assignmentEngine;
    }

    public async Task ToggleEngineerStatusAsync(int engineerId, bool isOnline, CancellationToken ct = default)
    {
        var engineer = await _users.GetByIdAsync(engineerId, ct)
            ?? throw new InvalidOperationException("Engineer not found.");

        if (engineer.Role != UserRole.Agent)
            throw new InvalidOperationException("Only engineers can toggle availability.");

        engineer.IsOnline = isOnline;
        engineer.IsAvailable = isOnline;
        engineer.LastSeenAt = DateTime.UtcNow;
        await _users.UpdateAsync(engineer, ct);

        if (isOnline && engineer.IsActive)
            await _assignmentEngine.AssignQueuedTicketsAsync(ct);
    }
}
