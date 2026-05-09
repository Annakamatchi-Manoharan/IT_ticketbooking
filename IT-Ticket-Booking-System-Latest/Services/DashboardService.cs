using System.Text.Json;
using ITBookingSystem.Repositories;
using ITBookingSystem.ViewModels;

namespace ITBookingSystem.Services;

public class DashboardService
{
    private readonly ITicketRepository _tickets;

    public DashboardService(ITicketRepository tickets)
    {
        _tickets = tickets;
    }

    public async Task<DashboardIndexVm> BuildAdminDashboardAsync(CancellationToken ct = default)
    {
        var stats = await _tickets.GetDashboardStatsAsync(ct);
        var startToday = DateTime.UtcNow.Date;
        var resolvedToday = await _tickets.CountResolvedSinceAsync(startToday, ct);
        var trend = await _tickets.GetTicketsCreatedPerDayAsync(7, ct);
        var activity = await _tickets.GetRecentActivityAsync(14, ct);
        var agentPerf = await _tickets.GetAgentPerformanceAsync(30, ct);
        var resolutionTrend = await _tickets.GetAvgResolutionHoursByDayAsync(14, ct);
        var slaCompliance = await _tickets.GetSlaCompliancePercentAsync(30, ct);

        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return new DashboardIndexVm
        {
            TotalTickets = stats.total,
            OpenTickets = stats.open,
            PendingTickets = stats.inProgress,
            EscalatedTickets = stats.escalated,
            ResolvedToday = resolvedToday,
            OverdueSla = stats.overdue,
            SlaCompliancePercent = Math.Round(slaCompliance, 1),
            TrendDaysUtc = trend.Select(t => t.dayUtc).ToList(),
            TicketsCreatedPerDay = trend.Select(t => t.count).ToList(),
            RecentActivity = activity.Select(a => new DashboardActivityItemVm
            {
                Kind = a.Kind,
                Message = a.Message,
                AtUtc = a.AtUtc
            }).ToList(),
            AgentPerformanceJson = JsonSerializer.Serialize(agentPerf.Select(a => new { a.AgentName, a.ResolvedCount, a.AvgResolutionHours }), jsonOpts),
            ResolutionTrendLabelsJson = JsonSerializer.Serialize(resolutionTrend.Select(r => r.dayUtc.ToLocalTime().ToString("MMM dd")).ToList(), jsonOpts),
            ResolutionTrendValuesJson = JsonSerializer.Serialize(resolutionTrend.Select(r => Math.Round(r.avgResolutionHours, 2)).ToList(), jsonOpts)
        };
    }
}
