using Microsoft.AspNetCore.SignalR;
using ProgressPath.Hubs;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Service for broadcasting real-time updates to clients via SignalR.
/// Implements IHubNotificationService to provide a clean abstraction over IHubContext.
/// REQ-RT-001: All real-time updates use SignalR.
/// </summary>
public class HubNotificationService : IHubNotificationService
{
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly ILogger<HubNotificationService> _logger;

    public HubNotificationService(
        IHubContext<ProgressHub> hubContext,
        ILogger<HubNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyProgressUpdateAsync(
        int sessionId,
        int groupId,
        int currentProgress,
        int totalSteps,
        bool isCompleted)
    {
        var groupChannel = $"group_{groupId}";
        var studentChannel = $"student_{sessionId}";

        // For percentage goals, CurrentProgress already stores the percentage (0-100)
        var percentage = Math.Clamp(currentProgress, 0, 100);

        var payload = new
        {
            SessionId = sessionId,
            Progress = currentProgress,
            TotalSteps = totalSteps,
            Percentage = percentage,
            IsCompleted = isCompleted
        };

        // Send to both group channel (teachers) and student channel (multi-tab sync)
        // REQ-GROUP-020: Multiple tabs for same session receive synchronized updates
        await Task.WhenAll(
            _hubContext.Clients.Group(groupChannel)
                .SendAsync(ProgressHub.ClientMethods.ReceiveProgress, payload),
            _hubContext.Clients.Group(studentChannel)
                .SendAsync(ProgressHub.ClientMethods.ReceiveProgress, payload));

        _logger.LogDebug(
            "Sent progress update to channels {GroupChannel} and {StudentChannel}: Session {SessionId} at {Percentage}%",
            groupChannel,
            studentChannel,
            sessionId,
            percentage);
    }

    /// <inheritdoc />
    public async Task NotifyNewMessageAsync(int sessionId, int groupId, ChatMessage message)
    {
        var groupChannel = $"group_{groupId}";
        var studentChannel = $"student_{sessionId}";

        // Extract only the needed properties to avoid serialization issues
        // with navigation properties (REQ-RT-004 optimistic UI with server confirmation)
        var payload = new
        {
            Id = message.Id,
            Content = message.Content,
            IsFromStudent = message.IsFromStudent,
            Timestamp = message.Timestamp,
            IsOffTopic = message.IsOffTopic,
            SignificantProgress = message.SignificantProgress,
            IsSystemMessage = message.IsSystemMessage
        };

        // Send to both channels in parallel
        await Task.WhenAll(
            _hubContext.Clients.Group(groupChannel)
                .SendAsync(ProgressHub.ClientMethods.ReceiveMessage, payload),
            _hubContext.Clients.Group(studentChannel)
                .SendAsync(ProgressHub.ClientMethods.ReceiveMessage, payload));

        _logger.LogDebug(
            "Sent message notification to channels {GroupChannel} and {StudentChannel}: Message {MessageId}",
            groupChannel,
            studentChannel,
            message.Id);
    }

    /// <inheritdoc />
    public async Task NotifyAlertAsync(int groupId, HelpAlert alert)
    {
        var channelName = $"group_{groupId}";

        // Include nickname from the navigation property if available
        var payload = new
        {
            Id = alert.Id,
            SessionId = alert.StudentSessionId,
            AlertType = alert.AlertType.ToString(),
            CreatedAt = alert.CreatedAt,
            Nickname = alert.StudentSession?.Nickname ?? "Unknown"
        };

        await _hubContext.Clients
            .Group(channelName)
            .SendAsync(ProgressHub.ClientMethods.ReceiveAlert, payload);

        _logger.LogInformation(
            "Sent alert notification to {Channel}: Alert {AlertId} ({AlertType}) for session {SessionId}",
            channelName,
            alert.Id,
            alert.AlertType,
            alert.StudentSessionId);
    }

    /// <inheritdoc />
    public async Task NotifyAlertResolvedAsync(int groupId, int alertId)
    {
        var channelName = $"group_{groupId}";

        await _hubContext.Clients
            .Group(channelName)
            .SendAsync(ProgressHub.ClientMethods.AlertResolved, alertId);

        _logger.LogInformation(
            "Sent alert resolved notification to {Channel}: Alert {AlertId}",
            channelName,
            alertId);
    }

    /// <inheritdoc />
    public async Task NotifyStudentResumedActivityAsync(int groupId, string nickname)
    {
        var channelName = $"group_{groupId}";

        await _hubContext.Clients
            .Group(channelName)
            .SendAsync(ProgressHub.ClientMethods.StudentResumedActivity, nickname);

        _logger.LogInformation(
            "Sent student resumed activity notification to {Channel}: {Nickname}",
            channelName,
            nickname);
    }

    /// <inheritdoc />
    public async Task NotifyStudentJoinedAsync(int groupId, StudentSession session)
    {
        var channelName = $"group_{groupId}";

        // Extract only needed properties for the payload
        var payload = new
        {
            Id = session.Id,
            Nickname = session.Nickname,
            CurrentProgress = session.CurrentProgress,
            IsCompleted = session.IsCompleted,
            JoinedAt = session.JoinedAt
        };

        await _hubContext.Clients
            .Group(channelName)
            .SendAsync(ProgressHub.ClientMethods.StudentJoined, payload);

        _logger.LogInformation(
            "Sent student joined notification to {Channel}: {Nickname} (Session {SessionId})",
            channelName,
            session.Nickname,
            session.Id);
    }
}
