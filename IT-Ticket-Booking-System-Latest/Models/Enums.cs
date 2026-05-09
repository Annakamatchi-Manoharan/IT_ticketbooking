using System.ComponentModel.DataAnnotations;

namespace ITBookingSystem.Models;

public enum UserRole
{
    [Display(Name = "User")]
    User = 0,
    [Display(Name = "Engineer")]
    Agent = 1,
    [Display(Name = "Admin")]
    Admin = 2
}

public enum AgentSkill
{
    Network = 0,
    Software = 1,
    Hardware = 2
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    /// <summary>SLA breached and escalated to senior coverage.</summary>
    Escalated = 3
}

public enum TicketAuditAction
{
    Created = 0,
    Assigned = 1,
    StatusChanged = 2,
    Escalated = 3,
    CommentAdded = 4
}

public enum ProblemType
{
    Network = 0,
    Software = 1,
    Hardware = 2
}

public enum WorkLocation
{
    [Display(Name = "Office")]
    Office = 0,
    [Display(Name = "WFH")]
    Wfh = 1
}

public enum QueueStatus
{
    PendingAssignment = 0,
    Assigned = 1,
    Resolved = 2
}

