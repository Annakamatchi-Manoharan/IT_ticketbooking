using System.ComponentModel.DataAnnotations;
using ITBookingSystem.Models;

namespace ITBookingSystem.ViewModels;

public class UserEditVm
{
    public int? Id { get; set; }

    [Required, MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    [Required]
    public UserRole Role { get; set; }

    public AgentSkill? Skill { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsActive { get; set; } = true;

    [MaxLength(120)]
    public string? Department { get; set; }

    [MaxLength(32)]
    public string? ContactNumber { get; set; }

    public bool IsSeniorAgent { get; set; }
}
