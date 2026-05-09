using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    public AgentSkill? Skill { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsOnline { get; set; }

    public DateTime? LastSeenAt { get; set; }

    /// <summary>Active Open + InProgress tickets for smart assignment ordering.</summary>
    public int ActiveTicketCount { get; set; }

    [MaxLength(120)]
    public string? Department { get; set; }

    [MaxLength(32)]
    public string? ContactNumber { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Eligible for SLA escalation reassignment.</summary>
    public bool IsSeniorAgent { get; set; }

    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<InAppNotification> Notifications { get; set; } = new List<InAppNotification>();
}

