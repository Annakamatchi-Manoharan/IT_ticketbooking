using ITBookingSystem.Repositories;

namespace ITBookingSystem.Services;

public class OverdueTicketBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OverdueTicketBackgroundService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    public OverdueTicketBackgroundService(IServiceProvider sp, ILogger<OverdueTicketBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _sp.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var escalation = scope.ServiceProvider.GetRequiredService<EscalationService>();

                var marked = await repo.MarkOverdueAsync(DateTime.UtcNow, stoppingToken);
                if (marked > 0)
                    _logger.LogInformation("Marked {Count} tickets as overdue.", marked);

                var escalated = await escalation.ProcessPendingEscalationsAsync(stoppingToken);
                if (escalated > 0)
                    _logger.LogInformation("Processed {Count} SLA escalations.", escalated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue / escalation worker failed.");
            }
        }
    }
}
