namespace ProgressPath.Models;

/// <summary>
/// Represents the role of a user in the system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Teacher role - can create groups, monitor student progress, and resolve help alerts.
    /// </summary>
    Teacher = 0
}

/// <summary>
/// Represents the type of goal for a group.
/// </summary>
public enum GoalType
{
    /// <summary>
    /// Binary goal - completed or not completed (displayed as checkmark).
    /// </summary>
    Binary = 0,

    /// <summary>
    /// Percentage goal - progress tracked as percentage (displayed as progress bar).
    /// </summary>
    Percentage = 1
}

/// <summary>
/// Represents the type of alert that can be raised for a student.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Off-topic alert - student has sent multiple off-topic messages without returning on-topic.
    /// </summary>
    OffTopic = 0,

    /// <summary>
    /// Inactivity alert - student has been inactive for the configured timeout period (10 minutes).
    /// </summary>
    Inactivity = 1
}
