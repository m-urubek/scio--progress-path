using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Background service that monitors student inactivity and creates alerts.
/// Implements two-step escalation per assignment requirements:
/// 1. First warning: After 5 minutes of inactivity, sends a nudge message to the student
/// 2. Teacher alert: After 10 minutes (5 more after warning), alerts the teacher
/// REQ-AI-012 through REQ-AI-016
/// </summary>
public class InactivityMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InactivityMonitorService> _logger;

    /// <summary>
    /// Time before sending first warning to student (in minutes).
    /// </summary>
    private const int WarningTimeoutMinutes = 5;

    /// <summary>
    /// Time before escalating to teacher alert after warning (in minutes).
    /// Total inactivity time = WarningTimeoutMinutes + AlertTimeoutMinutes = 10 minutes.
    /// </summary>
    private const int AlertTimeoutMinutes = 5;

    /// <summary>
    /// Check interval in milliseconds (1 minute).
    /// </summary>
    private const int CheckIntervalMs = 60000;

    /// <summary>
    /// Nudge message sent to student when they've been inactive.
    /// </summary>
    private const string InactivityWarningMessage = 
        "👋 Hey there! I noticed you've been quiet for a while. " +
        "Need any help with the current task? Feel free to ask questions or share your progress!";

    public InactivityMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<InactivityMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InactivityMonitorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForInactiveStudentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue running - don't crash the service
                _logger.LogError(ex, "Error during inactivity check");
            }

            try
            {
                await Task.Delay(CheckIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
        }

        _logger.LogInformation("InactivityMonitorService stopped");
    }

    /// <summary>
    /// Checks for inactive students and implements two-step escalation:
    /// 1. After 5 minutes inactive: send warning message to student
    /// 2. After 10 minutes inactive (5 after warning): alert teacher
    /// </summary>
    private async Task CheckForInactiveStudentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProgressPathDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
        var hubNotificationService = scope.ServiceProvider.GetRequiredService<IHubNotificationService>();

        var warningThreshold = DateTime.UtcNow.AddMinutes(-WarningTimeoutMinutes);
        var alertThreshold = DateTime.UtcNow.AddMinutes(-AlertTimeoutMinutes);

        // Find inactive sessions that are not completed
        var inactiveSessions = await dbContext.StudentSessions
            .Include(s => s.HelpAlerts)
            .Include(s => s.Group)
            .Where(s => !s.IsCompleted)
            .ToListAsync(stoppingToken);

        foreach (var session in inactiveSessions)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Calculate when activity started (last activity, or join time if never sent a message)
            var activityStartTime = session.LastActivityAt ?? session.JoinedAt;
            var inactiveMinutes = (int)(DateTime.UtcNow - activityStartTime).TotalMinutes;

            // Skip if not inactive long enough for warning yet
            if (activityStartTime > warningThreshold)
            {
                continue;
            }

            // Check if warning has been sent
            if (session.InactivityWarningSentAt == null)
            {
                // Step 1: Send warning message to student (5 minutes inactive)
                await SendInactivityWarningAsync(dbContext, hubNotificationService, session);
                
                _logger.LogInformation(
                    "Sent inactivity warning to session {SessionId} (student '{Nickname}') - inactive for {Minutes} minutes",
                    session.Id, session.Nickname, inactiveMinutes);
            }
            else
            {
                // Warning was sent - check if enough time has passed to escalate to teacher
                var timeSinceWarning = DateTime.UtcNow - session.InactivityWarningSentAt.Value;
                
                if (timeSinceWarning.TotalMinutes >= AlertTimeoutMinutes)
                {
                    // Check if there's already an unresolved inactivity alert
                    var hasUnresolvedInactivityAlert = session.HelpAlerts
                        .Any(a => a.AlertType == AlertType.Inactivity && !a.IsResolved);

                    if (!hasUnresolvedInactivityAlert)
                    {
                        // Step 2: Alert teacher (10 minutes total inactive)
                        var alert = await alertService.CreateInactivityAlertAsync(session.Id);

                        if (alert != null)
                        {
                            _logger.LogInformation(
                                "Created inactivity alert for session {SessionId} (student '{Nickname}') - inactive for {Minutes} minutes",
                                session.Id, session.Nickname, inactiveMinutes);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sends an inactivity warning message to the student's chat.
    /// This is a system message that nudges the student to continue working.
    /// </summary>
    private async Task SendInactivityWarningAsync(
        ProgressPathDbContext dbContext,
        IHubNotificationService hubNotificationService,
        StudentSession session)
    {
        // Create warning message as a system message (not from student, marked as system)
        var warningMessage = new ChatMessage
        {
            StudentSessionId = session.Id,
            Content = InactivityWarningMessage,
            IsFromStudent = false,
            IsSystemMessage = true,
            SignificantProgress = false,
            IsOffTopic = false,
            Timestamp = DateTime.UtcNow
        };

        dbContext.ChatMessages.Add(warningMessage);

        // Mark that warning was sent
        session.InactivityWarningSentAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        // Broadcast to student's chat in real-time
        await hubNotificationService.NotifyNewMessageAsync(
            session.Id, 
            session.GroupId, 
            warningMessage);
    }
}
