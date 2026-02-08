namespace ProgressPath.Services;

/// <summary>
/// Event arguments for progress update events.
/// </summary>
public class ProgressUpdateEventArgs : EventArgs
{
    public int SessionId { get; init; }
    public int GroupId { get; init; }
    public int CurrentProgress { get; init; }
    public int TotalSteps { get; init; }
    public bool IsCompleted { get; init; }
}

/// <summary>
/// Event arguments for student joined events.
/// </summary>
public class StudentJoinedEventArgs : EventArgs
{
    public int SessionId { get; init; }
    public int GroupId { get; init; }
    public string Nickname { get; init; } = string.Empty;
    public int CurrentProgress { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime JoinedAt { get; init; }
}

/// <summary>
/// Event arguments for alert events.
/// </summary>
public class AlertEventArgs : EventArgs
{
    public int AlertId { get; init; }
    public int SessionId { get; init; }
    public int GroupId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Event arguments for alert resolved events.
/// </summary>
public class AlertResolvedEventArgs : EventArgs
{
    public int AlertId { get; init; }
    public int GroupId { get; init; }
}

/// <summary>
/// In-process event service for real-time updates to Blazor Server components.
/// This avoids the complexity of loopback SignalR connections.
/// Components subscribe to events for specific groups and receive updates directly in-memory.
/// </summary>
public interface IProgressEventService
{
    /// <summary>
    /// Event fired when a student's progress is updated.
    /// </summary>
    event EventHandler<ProgressUpdateEventArgs>? OnProgressUpdate;

    /// <summary>
    /// Event fired when a new student joins a group.
    /// </summary>
    event EventHandler<StudentJoinedEventArgs>? OnStudentJoined;

    /// <summary>
    /// Event fired when a new alert is created.
    /// </summary>
    event EventHandler<AlertEventArgs>? OnAlert;

    /// <summary>
    /// Event fired when an alert is resolved.
    /// </summary>
    event EventHandler<AlertResolvedEventArgs>? OnAlertResolved;

    /// <summary>
    /// Publishes a progress update event.
    /// </summary>
    void PublishProgressUpdate(ProgressUpdateEventArgs args);

    /// <summary>
    /// Publishes a student joined event.
    /// </summary>
    void PublishStudentJoined(StudentJoinedEventArgs args);

    /// <summary>
    /// Publishes an alert event.
    /// </summary>
    void PublishAlert(AlertEventArgs args);

    /// <summary>
    /// Publishes an alert resolved event.
    /// </summary>
    void PublishAlertResolved(AlertResolvedEventArgs args);
}
