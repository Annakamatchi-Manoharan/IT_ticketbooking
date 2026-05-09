using ITBookingSystem.Models;

namespace ITBookingSystem.DTOs;

public class TicketListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public ProblemType ProblemType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime SLADeadline { get; set; }
    public bool IsOverdue { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public string? RequesterName { get; set; }
    public DateTime UpdatedAt { get; set; }
}
