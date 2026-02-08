namespace ProgressPath.Models;

/// <summary>
/// Represents a help alert raised for a student.
/// Teachers see these alerts on their dashboard and can mark them as resolved.
/// </summary>
public class HelpAlert
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the student session this alert is for.
    /// </summary>
    public int StudentSessionId { get; set; }

    /// <summary>
    /// Type of alert: OffTopic or Inactivity.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Whether the teacher has resolved this alert.
    /// After resolution, new alerts can be triggered (REQ-AI-018).
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// Timestamp when the alert was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the alert was resolved by the teacher.
    /// Null if not yet resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The student session this alert is associated with.
    /// </summary>
    public StudentSession StudentSession { get; set; } = null!;
}
