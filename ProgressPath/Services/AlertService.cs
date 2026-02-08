using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Implementation of alert management operations.
/// Handles off-topic detection, inactivity alerts, and alert resolution.
/// REQ-AI-006 through REQ-AI-020
/// </summary>
public class AlertService : IAlertService
{
    private readonly ProgressPathDbContext _dbContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        ProgressPathDbContext dbContext,
        ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessOffTopicMessageAsync(int sessionId)
    {
        var session = await _dbContext.StudentSessions
            .Include(s => s.Group)
            .Include(s => s.HelpAlerts)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            _logger.LogWarning("Attempted to process off-topic for non-existent session {SessionId}", sessionId);
            return;
        }

        // Increment the off-topic warning counter
        session.OffTopicWarningCount++;
        _logger.LogInformation(
            "Off-topic warning count for session {SessionId}: {Count}",
            sessionId, session.OffTopicWarningCount);

        // First off-topic (count = 1) is just a warning, no alert (REQ-AI-008)
        // Subsequent off-topic (count >= 2) creates an alert
        if (session.OffTopicWarningCount >= 2)
        {
            // Check if there's already an unresolved OffTopic alert for this session
            var existingAlert = session.HelpAlerts
                .Any(a => a.AlertType == AlertType.OffTopic && !a.IsResolved);

            if (!existingAlert)
            {
                // Create new off-topic alert
                var alert = new HelpAlert
                {
                    StudentSessionId = sessionId,
                    AlertType = AlertType.OffTopic,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.HelpAlerts.Add(alert);

                // Update session alert status
                session.HasActiveAlert = true;
                session.AlertType = AlertType.OffTopic;

                _logger.LogInformation(
                    "Created off-topic alert for session {SessionId} in group {GroupId}",
                    sessionId, session.GroupId);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<HelpAlert?> CreateInactivityAlertAsync(int sessionId)
    {
        var session = await _dbContext.StudentSessions
            .Include(s => s.HelpAlerts)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            _logger.LogWarning("Attempted to create inactivity alert for non-existent session {SessionId}", sessionId);
            return null;
        }

        // Check if there's already an unresolved inactivity alert (REQ-AI-016)
        var existingInactivityAlert = session.HelpAlerts
            .Any(a => a.AlertType == AlertType.Inactivity && !a.IsResolved);

        if (existingInactivityAlert)
        {
            _logger.LogDebug(
                "Skipping inactivity alert for session {SessionId}: unresolved alert already exists",
                sessionId);
            return null;
        }

        // Create new inactivity alert
        var alert = new HelpAlert
        {
            StudentSessionId = sessionId,
            AlertType = AlertType.Inactivity,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.HelpAlerts.Add(alert);

        // Update session alert status
        session.HasActiveAlert = true;
        session.AlertType = AlertType.Inactivity;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created inactivity alert {AlertId} for session {SessionId}",
            alert.Id, sessionId);

        return alert;
    }

    /// <inheritdoc />
    public async Task ResolveAlertAsync(int alertId)
    {
        var alert = await _dbContext.HelpAlerts
            .Include(a => a.StudentSession)
                .ThenInclude(s => s.HelpAlerts)
            .FirstOrDefaultAsync(a => a.Id == alertId);

        if (alert == null)
        {
            _logger.LogWarning("Attempted to resolve non-existent alert {AlertId}", alertId);
            return;
        }

        if (alert.IsResolved)
        {
            _logger.LogDebug("Alert {AlertId} is already resolved", alertId);
            return;
        }

        // Mark alert as resolved
        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;

        // Check if there are any other unresolved alerts for this session
        var hasOtherUnresolvedAlerts = alert.StudentSession.HelpAlerts
            .Any(a => a.Id != alertId && !a.IsResolved);

        if (!hasOtherUnresolvedAlerts)
        {
            // No other unresolved alerts, clear the session's alert status
            alert.StudentSession.HasActiveAlert = false;
            alert.StudentSession.AlertType = null;
        }
        else
        {
            // There are other unresolved alerts, update to the other alert type
            var otherAlert = alert.StudentSession.HelpAlerts
                .First(a => a.Id != alertId && !a.IsResolved);
            alert.StudentSession.AlertType = otherAlert.AlertType;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Resolved alert {AlertId} of type {AlertType} for session {SessionId}",
            alertId, alert.AlertType, alert.StudentSessionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HelpAlert>> GetUnresolvedAlertsAsync(int groupId)
    {
        return await _dbContext.HelpAlerts
            .Include(a => a.StudentSession)
            .Where(a => a.StudentSession.GroupId == groupId && !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ResetOffTopicCounterAsync(int sessionId)
    {
        var session = await _dbContext.StudentSessions.FindAsync(sessionId);

        if (session == null)
        {
            _logger.LogWarning("Attempted to reset off-topic counter for non-existent session {SessionId}", sessionId);
            return;
        }

        if (session.OffTopicWarningCount > 0)
        {
            _logger.LogDebug(
                "Resetting off-topic counter for session {SessionId} from {Count} to 0",
                sessionId, session.OffTopicWarningCount);

            session.OffTopicWarningCount = 0;
            await _dbContext.SaveChangesAsync();
        }
    }
}
