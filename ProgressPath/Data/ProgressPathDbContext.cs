using Microsoft.EntityFrameworkCore;
using ProgressPath.Models;

namespace ProgressPath.Data;

/// <summary>
/// Entity Framework DbContext for the Progress Path application.
/// Manages all database entities and their relationships.
/// </summary>
public class ProgressPathDbContext : DbContext
{
    public ProgressPathDbContext(DbContextOptions<ProgressPathDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Teacher users in the system.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Learning groups created by teachers.
    /// </summary>
    public DbSet<Group> Groups { get; set; } = null!;

    /// <summary>
    /// Student sessions within groups.
    /// </summary>
    public DbSet<StudentSession> StudentSessions { get; set; } = null!;

    /// <summary>
    /// Chat messages between students and the AI tutor.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    /// <summary>
    /// Help alerts raised for students needing teacher attention.
    /// </summary>
    public DbSet<HelpAlert> HelpAlerts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            // GoogleId must be unique (OAuth identifier)
            entity.HasIndex(u => u.GoogleId)
                .IsUnique();

            // Index on Email for lookups
            entity.HasIndex(u => u.Email);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            // JoinCode must be unique (used for QR codes and joining)
            entity.HasIndex(g => g.JoinCode)
                .IsUnique();

            // Relationship: Group belongs to a Teacher
            entity.HasOne(g => g.Teacher)
                .WithMany(u => u.Groups)
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StudentSession configuration
        modelBuilder.Entity<StudentSession>(entity =>
        {
            // Composite unique index: one device per group (REQ-GROUP-017)
            entity.HasIndex(s => new { s.GroupId, s.DeviceId })
                .IsUnique();

            // Unique nickname within group (REQ-GROUP-016)
            entity.HasIndex(s => new { s.GroupId, s.Nickname })
                .IsUnique();

            // Relationship: StudentSession belongs to a Group
            entity.HasOne(s => s.Group)
                .WithMany(g => g.StudentSessions)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            // Index for efficient session message queries
            entity.HasIndex(m => m.StudentSessionId);

            // Index for chronological ordering
            entity.HasIndex(m => m.Timestamp);

            // Relationship: ChatMessage belongs to a StudentSession
            entity.HasOne(m => m.StudentSession)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(m => m.StudentSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HelpAlert configuration
        modelBuilder.Entity<HelpAlert>(entity =>
        {
            // Index for session alert queries
            entity.HasIndex(a => a.StudentSessionId);

            // Index for filtering resolved/unresolved alerts
            entity.HasIndex(a => a.IsResolved);

            // Relationship: HelpAlert belongs to a StudentSession
            entity.HasOne(a => a.StudentSession)
                .WithMany(s => s.HelpAlerts)
                .HasForeignKey(a => a.StudentSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
