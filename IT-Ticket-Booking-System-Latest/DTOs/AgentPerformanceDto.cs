namespace ITBookingSystem.DTOs;

public class AgentPerformanceDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int ResolvedCount { get; set; }
    public double AvgResolutionHours { get; set; }
}
