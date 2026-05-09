using ITBookingSystem.DTOs;
using ITBookingSystem.Models;

namespace ITBookingSystem.ViewModels;

public class AssignedTicketsVm
{
    public PagedResult<TicketListItemDto> PagedTickets { get; set; } = new();
    public List<EngineerResolvedTicketDto> ResolvedTickets { get; set; } = new();
    public int ActiveTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedToday { get; set; }
    public int SlaBreaches { get; set; }
    public double? AvgResolutionHours { get; set; }
    public double SlaCompliancePercent { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public TicketPriority? Priority { get; set; }
    public ProblemType? ProblemType { get; set; }
    public string? SlaStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Query { get; set; }
}
