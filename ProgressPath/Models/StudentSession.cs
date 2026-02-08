using System.ComponentModel.DataAnnotations;

namespace ProgressPath.Models;

/// <summary>
/// Represents a student's session within a group.
/// Students are anonymous and identified by device ID and nickname.
/// </summary>
public class StudentSession
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the group this session belongs to.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Student's display name within the group.
    /// Must be unique within the group (REQ-GROUP-016).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Unique device identifier (typically from localStorage).
    /// Combined with GroupId forms a unique constraint to enforce
    /// one device per group (REQ-GROUP-017).
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Number of completed steps toward the goal.
    /// For percentage goals: percentage = (CurrentProgress / TotalSteps) * 100.
    /// For binary goals: 0 = incomplete, 1 = complete.
    /// Progress can only increase, never decrease (REQ-GOAL-011).
    /// </summary>
    public int CurrentProgress { get; set; } = 0;

    /// <summary>
    /// Whether the student has completed the goal (100% or binary complete).
    /// When true, chat input is disabled (REQ-GOAL-014).
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Counter for off-topic messages without returning on-topic.
    /// First off-topic = warning, subsequent = escalation to help alert.
    /// Resets when student returns to on-topic (REQ-AI-010).
    /// </summary>
    public int OffTopicWarningCount { get; set; } = 0;

    /// <summary>
    /// Whether the student currently has an unresolved help alert.
    /// </summary>
    public bool HasActiveAlert { get; set; } = false;

    /// <summary>
    /// Type of the current active alert, if any.
    /// Null when HasActiveAlert is false.
    /// </summary>
    public AlertType? AlertType { get; set; }

    /// <summary>
    /// Timestamp of the student's last activity (message sent).
    /// Used for inactivity detection (10-minute timeout per REQ-AI-012).
    /// Null if no messages have been sent yet.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Timestamp when the student joined the group.
    /// Inactivity timer starts from this point (REQ-AI-013).
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The group this session belongs to.
    /// </summary>
    public Group Group { get; set; } = null!;

    /// <summary>
    /// Chat messages in this session.
    /// </summary>
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Help alerts raised for this student.
    /// </summary>
    public ICollection<HelpAlert> HelpAlerts { get; set; } = new List<HelpAlert>();
}
