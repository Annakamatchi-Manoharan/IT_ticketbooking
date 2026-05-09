namespace ITBookingSystem.ViewModels;

public class DashboardIndexVm
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int PendingTickets { get; set; }
    public int EscalatedTickets { get; set; }
    public int ResolvedToday { get; set; }
    public int OverdueSla { get; set; }
    public double SlaCompliancePercent { get; set; }

    public List<DateTime> TrendDaysUtc { get; set; } = new();
    public List<int> TicketsCreatedPerDay { get; set; } = new();

    public List<DashboardActivityItemVm> RecentActivity { get; set; } = new();

    public string AgentPerformanceJson { get; set; } = "[]";
    public string ResolutionTrendLabelsJson { get; set; } = "[]";
    public string ResolutionTrendValuesJson { get; set; } = "[]";
}

public class DashboardActivityItemVm
{
    public string Kind { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
}
