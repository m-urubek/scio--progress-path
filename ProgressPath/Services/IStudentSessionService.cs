using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Interface for student session management operations.
/// Handles student joining groups, session restoration, and progress tracking.
/// REQ-GROUP-014 through REQ-GROUP-024
/// </summary>
public interface IStudentSessionService
{
    /// <summary>
    /// Joins a student to a group with the specified nickname and device binding.
    /// If the device already has a session in this group, returns the existing session (REQ-GROUP-018).
    /// REQ-GROUP-014, REQ-GROUP-015, REQ-GROUP-016, REQ-GROUP-017
    /// </summary>
    /// <param name="joinCode">The 6-8 character alphanumeric join code (case-insensitive).</param>
    /// <param name="nickname">Student's display name. Must be unique within the group.</param>
    /// <param name="deviceId">Unique device identifier from localStorage.</param>
    /// <returns>The new or restored student session.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the group with the specified join code is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the group is not yet confirmed by the teacher (REQ-GROUP-010).</exception>
    /// <exception cref="ArgumentException">Thrown if the nickname is already taken in the group (REQ-GROUP-016).</exception>
    Task<StudentSession> JoinGroupAsync(string joinCode, string nickname, string deviceId);

    /// <summary>
    /// Retrieves an existing session for a device in a group.
    /// Used for session restoration when a device revisits a group (REQ-GROUP-018, REQ-GROUP-020).
    /// </summary>
    /// <param name="joinCode">The 6-8 character alphanumeric join code (case-insensitive).</param>
    /// <param name="deviceId">Unique device identifier from localStorage.</param>
    /// <returns>The existing session if found, null otherwise.</returns>
    Task<StudentSession?> GetExistingSessionAsync(string joinCode, string deviceId);

    /// <summary>
    /// Checks if a nickname is available within a group.
    /// Comparison is case-insensitive (REQ-GROUP-016).
    /// </summary>
    /// <param name="groupId">ID of the group to check.</param>
    /// <param name="nickname">Nickname to check availability for.</param>
    /// <returns>True if the nickname is available, false if already taken.</returns>
    Task<bool> IsNicknameAvailableAsync(int groupId, string nickname);

    /// <summary>
    /// Updates a student's progress toward the goal.
    /// Progress can only increase, never decrease (REQ-GOAL-011).
    /// Automatically marks session as completed when goal is reached.
    /// </summary>
    /// <param name="sessionId">ID of the student session.</param>
    /// <param name="newProgress">New progress value (number of steps completed).</param>
    /// <exception cref="KeyNotFoundException">Thrown if the session is not found.</exception>
    Task UpdateProgressAsync(int sessionId, int newProgress);

    /// <summary>
    /// Updates the last activity timestamp for a session.
    /// Used for inactivity detection (REQ-AI-013).
    /// </summary>
    /// <param name="sessionId">ID of the student session.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the session is not found.</exception>
    Task UpdateLastActivityAsync(int sessionId);

    /// <summary>
    /// Retrieves all student sessions for a group.
    /// Used for the teacher dashboard display (REQ-DASH-004).
    /// </summary>
    /// <param name="groupId">ID of the group.</param>
    /// <returns>All student sessions in the group, ordered by join date descending.</returns>
    Task<IEnumerable<StudentSession>> GetGroupSessionsAsync(int groupId);
}
