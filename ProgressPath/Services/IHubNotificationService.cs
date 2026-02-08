using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Interface for broadcasting real-time updates to clients via SignalR.
/// All notifications go through this service rather than directly using IHubContext.
/// REQ-RT-001: All real-time updates use SignalR.
/// </summary>
public interface IHubNotificationService
{
    /// <summary>
    /// Broadcasts a progress update to the group channel.
    /// Teachers on the dashboard receive this notification.
    /// REQ-DASH-005: Progress updates appear in real-time without page refresh.
    /// </summary>
    /// <param name="sessionId">The student session ID.</param>
    /// <param name="groupId">The group ID (determines the channel).</param>
    /// <param name="currentProgress">The student's current progress (completed steps).</param>
    /// <param name="totalSteps">Total steps for the goal (1 for binary goals).</param>
    /// <param name="isCompleted">Whether the student has completed the goal.</param>
    Task NotifyProgressUpdateAsync(int sessionId, int groupId, int currentProgress, int totalSteps, bool isCompleted);

    /// <summary>
    /// Broadcasts a new chat message to both the group channel and the student's channel.
    /// Teachers and the student (in other tabs) receive this notification.
    /// REQ-GROUP-020: Multiple tabs receive synchronized updates.
    /// </summary>
    /// <param name="sessionId">The student session ID.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="message">The chat message to broadcast.</param>
    Task NotifyNewMessageAsync(int sessionId, int groupId, ChatMessage message);

    /// <summary>
    /// Broadcasts a new help alert to the group channel.
    /// Teachers see the alert indicator on their dashboard.
    /// REQ-DASH-006: Students requiring help display a visible alert indicator.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="alert">The help alert to broadcast.</param>
    Task NotifyAlertAsync(int groupId, HelpAlert alert);

    /// <summary>
    /// Broadcasts an alert resolution to the group channel.
    /// Teachers see the alert dismissed from the dashboard.
    /// REQ-DASH-007: Teachers can mark help alerts as resolved.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="alertId">The ID of the resolved alert.</param>
    Task NotifyAlertResolvedAsync(int groupId, int alertId);

    /// <summary>
    /// Broadcasts a toast notification when a previously inactive student resumes activity.
    /// REQ-AI-017: 4-second auto-dismiss toast with student's nickname.
    /// REQ-DASH-012: Activity resumption notifications display nickname and auto-dismiss.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="nickname">The student's nickname.</param>
    Task NotifyStudentResumedActivityAsync(int groupId, string nickname);

    /// <summary>
    /// Broadcasts when a new student joins the group.
    /// Teachers see the new student on their dashboard.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="session">The student session that joined.</param>
    Task NotifyStudentJoinedAsync(int groupId, StudentSession session);
}
