using ITBookingSystem.Models;

namespace ITBookingSystem.DTOs;

public class EngineerResolvedTicketDto
{
    public int Id { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string FinalComment { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public TicketPriority Priority { get; set; }
    public bool SlaMet { get; set; }
}
