using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Background service that monitors student inactivity and creates alerts.
/// Runs periodic checks every minute to detect students inactive for 10+ minutes.
/// REQ-AI-012 through REQ-AI-016
/// </summary>
public class InactivityMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InactivityMonitorService> _logger;

    /// <summary>
    /// Inactivity timeout in minutes (fixed at 10 per REQ-AI-012 / FC-001).
    /// </summary>
    private const int InactivityTimeoutMinutes = 10;

    /// <summary>
    /// Check interval in milliseconds (1 minute).
    /// </summary>
    private const int CheckIntervalMs = 60000;

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
    /// Checks for inactive students and creates alerts for those who meet the criteria.
    /// </summary>
    private async Task CheckForInactiveStudentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProgressPathDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        var inactivityThreshold = DateTime.UtcNow.AddMinutes(-InactivityTimeoutMinutes);

        // Find inactive sessions:
        // 1. Not completed (completed students don't need monitoring)
        // 2. Either:
        //    a. No messages sent yet (LastActivityAt is null) AND joined > 10 minutes ago
        //    b. Has sent messages AND last activity > 10 minutes ago
        // 3. Don't already have an unresolved inactivity alert
        var inactiveSessions = await dbContext.StudentSessions
            .Include(s => s.HelpAlerts)
            .Where(s =>
                !s.IsCompleted &&
                (
                    // Never sent a message, but joined > 10 minutes ago
                    (s.LastActivityAt == null && s.JoinedAt < inactivityThreshold) ||
                    // Has sent messages, but last activity > 10 minutes ago
                    (s.LastActivityAt != null && s.LastActivityAt < inactivityThreshold)
                ))
            .ToListAsync(stoppingToken);

        if (inactiveSessions.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Found {Count} potentially inactive sessions to check", inactiveSessions.Count);

        foreach (var session in inactiveSessions)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Check if there's already an unresolved inactivity alert for this session
            var hasUnresolvedInactivityAlert = session.HelpAlerts
                .Any(a => a.AlertType == AlertType.Inactivity && !a.IsResolved);

            if (hasUnresolvedInactivityAlert)
            {
                // Skip - alert already exists (REQ-AI-016)
                continue;
            }

            // Create inactivity alert
            var alert = await alertService.CreateInactivityAlertAsync(session.Id);

            if (alert != null)
            {
                var inactiveMinutes = session.LastActivityAt.HasValue
                    ? (int)(DateTime.UtcNow - session.LastActivityAt.Value).TotalMinutes
                    : (int)(DateTime.UtcNow - session.JoinedAt).TotalMinutes;

                _logger.LogInformation(
                    "Created inactivity alert for session {SessionId} (student '{Nickname}') - inactive for {Minutes} minutes",
                    session.Id, session.Nickname, inactiveMinutes);
            }
        }
    }
}
