using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

/// <summary>Self-service troubleshooting content for the user support bot and engineer assistant.</summary>
public class SupportFaq
{
    public int Id { get; set; }

    /// <summary>Display category, e.g. "VPN Issue" or "Network".</summary>
    [Required, MaxLength(120)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string IssueTitle { get; set; } = string.Empty;

    /// <summary>JSON array of step strings shown one-by-one in the user bot.</summary>
    [Required]
    public string StepsJson { get; set; } = "[]";

    /// <summary>Ticket form mapping when user escalates to Create Ticket.</summary>
    public ProblemType ProblemType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
