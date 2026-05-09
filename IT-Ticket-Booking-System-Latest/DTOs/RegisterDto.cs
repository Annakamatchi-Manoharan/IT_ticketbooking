using System.ComponentModel.DataAnnotations;
using ITBookingSystem.Models;

namespace ITBookingSystem.DTOs;

public class RegisterDto
{
    [Required, MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;
    public AgentSkill? Skill { get; set; }
}

