using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (Exception)
        {
            // Migration failure should not crash the app in dev - swallow and allow fallback
        }

        // Ensure Users table exists and is accessible
        try
        {
            if (await db.Users.AnyAsync(ct))
                return;
        }
        catch (Exception)
        {
            // If table doesn't exist or DB not reachable, skip seeding
            return;
        }

        var admin = new User
        {
            Username = "Admin",
            Email = "admin@it.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            IsAvailable = true,
            IsOnline = false,
            IsActive = true,
            ActiveTicketCount = 0,
            Department = "IT Operations",
            ContactNumber = "+1-555-0100"
        };

        var agents = new[]
        {
            new User { Username = "Alex Morgan", Email = "alex.agent@it.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@123"), Role = UserRole.Agent, Skill = AgentSkill.Network, IsAvailable = true, IsOnline = false, IsActive = true, ActiveTicketCount = 0, Department = "NOC", ContactNumber = "+1-555-0101", IsSeniorAgent = true },
            new User { Username = "Sarah Johnson", Email = "sarah.agent@it.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@123"), Role = UserRole.Agent, Skill = AgentSkill.Software, IsAvailable = true, IsOnline = false, IsActive = true, ActiveTicketCount = 0, Department = "Apps", ContactNumber = "+1-555-0102" },
            new User { Username = "Michael Lee", Email = "michael.agent@it.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@123"), Role = UserRole.Agent, Skill = AgentSkill.Hardware, IsAvailable = true, IsOnline = false, IsActive = true, ActiveTicketCount = 0, Department = "Field", ContactNumber = "+1-555-0103" }
        };

        var demoUser = new User
        {
            Username = "Demo User",
            Email = "user@it.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
            Role = UserRole.User,
            IsAvailable = true,
            IsOnline = false,
            IsActive = true,
            ActiveTicketCount = 0,
            Department = "Finance",
            ContactNumber = "+1-555-0199"
        };

        try
        {
            db.Users.Add(admin);
            db.Users.AddRange(agents);
            db.Users.Add(demoUser);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            // Ignore seeding errors to avoid startup failure
        }

        // Ensure there is at least one sample ticket so dashboard pages that expect tickets don't crash
        try
        {
            var anyTicket = await db.Tickets.AnyAsync(ct);
            if (!anyTicket)
            {
                var ticket = new Ticket
                {
                    Title = "Welcome - Sample Ticket",
                    Description = "This is a seeded demo ticket. Delete as needed.",
                    CreatedAt = DateTime.UtcNow,
                    Status = TicketStatus.Open,
                    QueueStatus = QueueStatus.PendingAssignment,
                    UserId = demoUser.Id
                };

                db.Tickets.Add(ticket);
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception)
        {
            // ignore
        }
    }

    /// <summary>Keeps agent <see cref="User.ActiveTicketCount"/> aligned with Open/InProgress assignments.</summary>
    public static async Task SyncAgentWorkloadAsync(AppDbContext db, CancellationToken ct = default)
    {
        try
        {
            var agents = await db.Users.Where(u => u.Role == UserRole.Agent).ToListAsync(ct);
            foreach (var a in agents)
            {
                a.ActiveTicketCount = await db.Tickets.CountAsync(t =>
                    t.AssignedAgentId == a.Id &&
                    (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Escalated), ct);
            }

            if (agents.Count > 0)
                await db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            // ignore sync failures
        }
    }
}
