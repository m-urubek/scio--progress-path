using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Interface for alert management operations.
/// Handles off-topic detection, inactivity alerts, and alert resolution.
/// REQ-AI-006 through REQ-AI-020
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Processes an off-topic message for a student session.
    /// First off-topic message increments the warning counter (no alert).
    /// Subsequent off-topic messages without returning on-topic create an alert.
    /// REQ-AI-008, REQ-AI-009, REQ-AI-019
    /// </summary>
    /// <param name="sessionId">ID of the student session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessOffTopicMessageAsync(int sessionId);

    /// <summary>
    /// Creates an inactivity alert for a student session if no unresolved inactivity alert exists.
    /// REQ-AI-015, REQ-AI-016, REQ-AI-020
    /// </summary>
    /// <param name="sessionId">ID of the student session.</param>
    /// <returns>The created alert, or null if an unresolved inactivity alert already exists.</returns>
    Task<HelpAlert?> CreateInactivityAlertAsync(int sessionId);

    /// <summary>
    /// Resolves an alert by marking it as resolved and updating timestamps.
    /// Allows new alerts to be triggered after resolution (REQ-AI-018).
    /// REQ-AI-018, REQ-AI-019, REQ-AI-020
    /// </summary>
    /// <param name="alertId">ID of the alert to resolve.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResolveAlertAsync(int alertId);

    /// <summary>
    /// Retrieves all unresolved help alerts for a group.
    /// Used for the teacher dashboard display (REQ-DASH-006).
    /// </summary>
    /// <param name="groupId">ID of the group.</param>
    /// <returns>All unresolved alerts for students in the group.</returns>
    Task<IEnumerable<HelpAlert>> GetUnresolvedAlertsAsync(int groupId);

    /// <summary>
    /// Resets the off-topic warning counter for a session.
    /// Called when an on-topic message is received (REQ-AI-010).
    /// </summary>
    /// <param name="sessionId">ID of the student session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetOffTopicCounterAsync(int sessionId);
}
