using System.ComponentModel.DataAnnotations;

namespace ProgressPath.Models;

/// <summary>
/// Represents a chat message in a student's session.
/// Messages can be from the student or the AI tutor.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the student session this message belongs to.
    /// </summary>
    public int StudentSessionId { get; set; }

    /// <summary>
    /// The text content of the message.
    /// May contain LaTeX math expressions or code blocks.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// True if the message was sent by the student, false if from the AI tutor.
    /// </summary>
    public bool IsFromStudent { get; set; }

    /// <summary>
    /// Whether this message contributed to goal progress (REQ-AI-028).
    /// Pre-computed during AI message processing.
    /// Used to highlight contributing messages and show progress on teacher dashboard.
    /// </summary>
    public bool ContributesToProgress { get; set; } = false;

    /// <summary>
    /// Whether this message was classified as off-topic.
    /// Only applicable to student messages.
    /// </summary>
    public bool IsOffTopic { get; set; } = false;

    /// <summary>
    /// Timestamp when the message was created.
    /// Messages are displayed in chronological order.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The student session this message belongs to.
    /// </summary>
    public StudentSession StudentSession { get; set; } = null!;
}
