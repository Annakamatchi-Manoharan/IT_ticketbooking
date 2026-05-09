using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public class TicketHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    [Required]
    public TicketAuditAction Action { get; set; }

    /// <summary>Human-readable audit line (plain text).</summary>
    [Required, MaxLength(2000)]
    public string Details { get; set; } = string.Empty;

    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
