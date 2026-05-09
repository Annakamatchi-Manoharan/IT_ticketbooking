using ITBookingSystem.Models;

namespace ITBookingSystem.Services;

public class SLAService
{
    public DateTime CalculateDeadlineUtc(TicketPriority priority, DateTime createdAtUtc)
    {
        var hours = priority switch
        {
            TicketPriority.Critical => 1,
            TicketPriority.High => 1,
            TicketPriority.Medium => 4,
            TicketPriority.Low => 24,
            _ => 24
        };

        return createdAtUtc.AddHours(hours);
    }

    public AgentSkill MapProblemToSkill(ProblemType problemType)
    {
        return problemType switch
        {
            ProblemType.Network => AgentSkill.Network,
            ProblemType.Software => AgentSkill.Software,
            ProblemType.Hardware => AgentSkill.Hardware,
            _ => AgentSkill.Software
        };
    }
}

