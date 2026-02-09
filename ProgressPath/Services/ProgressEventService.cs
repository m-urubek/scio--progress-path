namespace ProgressPath.Services;

/// <summary>
/// Implementation of in-process event service for real-time updates.
/// Uses standard .NET events for efficient in-memory notifications.
/// This is registered as a Singleton so all components share the same instance.
/// </summary>
public class ProgressEventService : IProgressEventService
{
    private readonly ILogger<ProgressEventService> _logger;

    public ProgressEventService(ILogger<ProgressEventService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<ProgressUpdateEventArgs>? OnProgressUpdate;

    /// <inheritdoc />
    public event EventHandler<StudentJoinedEventArgs>? OnStudentJoined;

    /// <inheritdoc />
    public event EventHandler<AlertEventArgs>? OnAlert;

    /// <inheritdoc />
    public event EventHandler<AlertResolvedEventArgs>? OnAlertResolved;

    /// <inheritdoc />
    public event EventHandler<MessageEventArgs>? OnMessage;

    /// <inheritdoc />
    public void PublishProgressUpdate(ProgressUpdateEventArgs args)
    {
        _logger.LogDebug(
            "Publishing progress update: Session {SessionId}, Group {GroupId}, Progress {Progress}",
            args.SessionId, args.GroupId, args.CurrentProgress);

        OnProgressUpdate?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void PublishStudentJoined(StudentJoinedEventArgs args)
    {
        _logger.LogDebug(
            "Publishing student joined: Session {SessionId}, Group {GroupId}, Nickname {Nickname}",
            args.SessionId, args.GroupId, args.Nickname);

        OnStudentJoined?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void PublishAlert(AlertEventArgs args)
    {
        _logger.LogDebug(
            "Publishing alert: Alert {AlertId}, Session {SessionId}, Group {GroupId}, Type {AlertType}",
            args.AlertId, args.SessionId, args.GroupId, args.AlertType);

        OnAlert?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void PublishAlertResolved(AlertResolvedEventArgs args)
    {
        _logger.LogDebug(
            "Publishing alert resolved: Alert {AlertId}, Group {GroupId}",
            args.AlertId, args.GroupId);

        OnAlertResolved?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void PublishMessage(MessageEventArgs args)
    {
        _logger.LogDebug(
            "Publishing message: Message {MessageId}, Session {SessionId}, System: {IsSystem}",
            args.MessageId, args.SessionId, args.IsSystemMessage);

        OnMessage?.Invoke(this, args);
    }
}
