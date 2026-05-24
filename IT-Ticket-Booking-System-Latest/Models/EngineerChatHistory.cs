using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public class EngineerChatHistory
{
    public int Id { get; set; }

    public int EngineerId { get; set; }
    public User Engineer { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Query { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? PredictedCategory { get; set; }

    /// <summary>JSON payload: causes, steps, recommendedActions, bestPractices.</summary>
    public string? BotResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
