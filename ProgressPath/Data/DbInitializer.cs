using Microsoft.EntityFrameworkCore;
using ProgressPath.Models;

namespace ProgressPath.Data;

/// <summary>
/// Database initializer for seeding development data.
/// Only runs in development environment and is idempotent (checks if data exists before inserting).
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with seed data for development.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ProgressPathDbContext>();

        // Check if there's already data in the database
        if (context.Users.Any())
        {
            // Database has been seeded
            return;
        }

        // Seed test teacher
        var teacher = SeedTestTeacher(context);

        // Seed test group
        var group = SeedTestGroup(context, teacher);

        // Seed test student sessions
        SeedTestStudentSessions(context, group);

        context.SaveChanges();
    }

    /// <summary>
    /// Creates a test teacher user for development.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>The created teacher user.</returns>
    private static User SeedTestTeacher(ProgressPathDbContext context)
    {
        var teacher = new User
        {
            Email = "test.teacher@example.com",
            DisplayName = "Test Teacher",
            GoogleId = "test-teacher-google-id-12345",
            Role = UserRole.Teacher,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Users.Add(teacher);
        context.SaveChanges();

        return teacher;
    }

    /// <summary>
    /// Creates a sample confirmed group for development.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="teacher">The teacher who owns the group.</param>
    /// <returns>The created group.</returns>
    private static Group SeedTestGroup(ProgressPathDbContext context, User teacher)
    {
        var group = new Group
        {
            Name = "A2 - Quadratic Equations",
            JoinCode = "MATH42",
            GoalDescription = "Independently solve 3 different quadratic equations of the type ax² + bx + c = 0 using the discriminant formula.",
            GoalType = GoalType.Percentage,
            TotalSteps = 3,
            IsConfirmed = true,
            WelcomeMessage = "Welcome! Your goal is to solve 3 different quadratic equations using the discriminant formula (b² - 4ac). I'll guide you through each step without giving direct answers. Let's start with understanding what makes an equation quadratic. Ready?",
            GoalInterpretation = "{\"type\":\"percentage\",\"steps\":3,\"stepDescriptions\":[\"Solve first quadratic equation\",\"Solve second quadratic equation\",\"Solve third quadratic equation\"]}",
            TeacherId = teacher.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        context.Groups.Add(group);
        context.SaveChanges();

        return group;
    }

    /// <summary>
    /// Creates sample student sessions with varying progress levels.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="group">The group the students belong to.</param>
    private static void SeedTestStudentSessions(ProgressPathDbContext context, Group group)
    {
        // Student 1: Completed
        var student1 = new StudentSession
        {
            GroupId = group.Id,
            Nickname = "Alice K.",
            DeviceId = "dev-alice-12345",
            CurrentProgress = 3,
            IsCompleted = true,
            OffTopicWarningCount = 0,
            HasActiveAlert = false,
            JoinedAt = DateTime.UtcNow.AddDays(-5),
            LastActivityAt = DateTime.UtcNow.AddDays(-4)
        };

        // Student 2: In progress (2/3)
        var student2 = new StudentSession
        {
            GroupId = group.Id,
            Nickname = "Bob M.",
            DeviceId = "dev-bob-67890",
            CurrentProgress = 2,
            IsCompleted = false,
            OffTopicWarningCount = 0,
            HasActiveAlert = false,
            JoinedAt = DateTime.UtcNow.AddDays(-3),
            LastActivityAt = DateTime.UtcNow.AddHours(-2)
        };

        // Student 3: Just started (0/3) with inactivity alert
        var student3 = new StudentSession
        {
            GroupId = group.Id,
            Nickname = "Charlie N.",
            DeviceId = "dev-charlie-11111",
            CurrentProgress = 0,
            IsCompleted = false,
            OffTopicWarningCount = 0,
            HasActiveAlert = true,
            AlertType = AlertType.Inactivity,
            JoinedAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = null
        };

        context.StudentSessions.AddRange(student1, student2, student3);
        context.SaveChanges();

        // Add sample chat messages for student 1
        var messages1 = new List<ChatMessage>
        {
            new ChatMessage
            {
                StudentSessionId = student1.Id,
                Content = "Welcome! Your goal is to solve 3 different quadratic equations using the discriminant formula (b² - 4ac). I'll guide you through each step without giving direct answers. Let's start with understanding what makes an equation quadratic. Ready?",
                IsFromStudent = false,
                SignificantProgress = false,
                IsOffTopic = false,
                Timestamp = DateTime.UtcNow.AddDays(-5)
            },
            new ChatMessage
            {
                StudentSessionId = student1.Id,
                Content = "Yes, I'm ready! I know quadratic equations have x squared in them.",
                IsFromStudent = true,
                SignificantProgress = false,
                IsOffTopic = false,
                Timestamp = DateTime.UtcNow.AddDays(-5).AddMinutes(2)
            },
            new ChatMessage
            {
                StudentSessionId = student1.Id,
                Content = "For x² + 5x + 6 = 0, I calculated b² - 4ac = 25 - 24 = 1, so x = (-5 ± 1)/2, giving x = -2 or x = -3.",
                IsFromStudent = true,
                SignificantProgress = true,
                IsOffTopic = false,
                Timestamp = DateTime.UtcNow.AddDays(-5).AddMinutes(15)
            }
        };

        // Add sample chat messages for student 2
        var messages2 = new List<ChatMessage>
        {
            new ChatMessage
            {
                StudentSessionId = student2.Id,
                Content = "Welcome! Your goal is to solve 3 different quadratic equations using the discriminant formula. Let's get started!",
                IsFromStudent = false,
                SignificantProgress = false,
                IsOffTopic = false,
                Timestamp = DateTime.UtcNow.AddDays(-3)
            },
            new ChatMessage
            {
                StudentSessionId = student2.Id,
                Content = "I solved 2x² - 3x - 2 = 0. The discriminant is 9 + 16 = 25, so x = (3 ± 5)/4, giving x = 2 or x = -0.5",
                IsFromStudent = true,
                SignificantProgress = true,
                IsOffTopic = false,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };

        context.ChatMessages.AddRange(messages1);
        context.ChatMessages.AddRange(messages2);

        // Add help alert for student 3
        var alert = new HelpAlert
        {
            StudentSessionId = student3.Id,
            AlertType = AlertType.Inactivity,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        context.HelpAlerts.Add(alert);
        context.SaveChanges();
    }
}
