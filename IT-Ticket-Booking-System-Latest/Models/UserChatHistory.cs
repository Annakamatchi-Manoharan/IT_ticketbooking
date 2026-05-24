using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public class UserChatHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string SearchedIssue { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
