using System.ComponentModel.DataAnnotations;
using ITBookingSystem.Models;

namespace ITBookingSystem.DTOs;

public class TicketCreateDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ProblemType ProblemType { get; set; }

    [Required]
    public TicketPriority Priority { get; set; }

    [Required]
    public WorkLocation WorkLocation { get; set; } = WorkLocation.Office;

    [MaxLength(64)]
    public string? TeamViewerId { get; set; }

    [MaxLength(200)]
    public string? TeamViewerPassword { get; set; }

    [MaxLength(32)]
    public string? ContactNumber { get; set; }

    [MaxLength(255), EmailAddress]
    public string? Email { get; set; }
}
