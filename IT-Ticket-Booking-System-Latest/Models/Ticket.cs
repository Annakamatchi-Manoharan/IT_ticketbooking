using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ProblemType ProblemType { get; set; }

    [Required]
    public TicketPriority Priority { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime SLADeadline { get; set; }

    public bool IsOverdue { get; set; }

    [Required]
    public WorkLocation WorkLocation { get; set; } = WorkLocation.Office;

    [MaxLength(64)]
    public string? TeamViewerId { get; set; }

    /// <summary>Data-protection encrypted payload (never store plaintext).</summary>
    public string? TeamViewerPasswordProtected { get; set; }

    [MaxLength(32)]
    public string? RequesterPhone { get; set; }

    [MaxLength(255)]
    public string? RequesterEmail { get; set; }

    /// <summary>Semicolon-separated relative paths under wwwroot for uploaded files.</summary>
    [MaxLength(4000)]
    public string? AttachmentPaths { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(2000)]
    public string? ResolutionComment { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Prevents duplicate SLA escalation processing.</summary>
    public bool EscalationProcessed { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? AssignedAgentId { get; set; }
    public User? AssignedAgent { get; set; }

    public DateTime? AssignedAt { get; set; }

    [Required]
    public QueueStatus QueueStatus { get; set; } = QueueStatus.PendingAssignment;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}

