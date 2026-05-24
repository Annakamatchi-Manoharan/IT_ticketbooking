using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<InAppNotification> Notifications => Set<InAppNotification>();
    public DbSet<SupportFaq> SupportFaqs => Set<SupportFaq>();
    public DbSet<UserChatHistory> UserChatHistories => Set<UserChatHistory>();
    public DbSet<EngineerChatHistory> EngineerChatHistories => Set<EngineerChatHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.User)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.AssignedAgent)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedAgentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Ticket)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketHistory>()
            .HasOne(h => h.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketHistory>()
            .HasOne(h => h.ActorUser)
            .WithMany()
            .HasForeignKey(h => h.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InAppNotification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.IsOverdue, t.EscalationProcessed, t.Status });

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.QueueStatus, t.AssignedAgentId, t.Status });

        modelBuilder.Entity<SupportFaq>()
            .HasIndex(f => f.Category);

        modelBuilder.Entity<UserChatHistory>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserChatHistory>()
            .HasIndex(h => new { h.UserId, h.CreatedAt });

        modelBuilder.Entity<EngineerChatHistory>()
            .HasOne(h => h.Engineer)
            .WithMany()
            .HasForeignKey(h => h.EngineerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EngineerChatHistory>()
            .HasIndex(h => new { h.EngineerId, h.CreatedAt });
    }
}
