namespace ProgressPath.Models.DTOs;

/// <summary>
/// Represents the AI's response to a student message.
/// Contains the response text along with classification metadata.
/// REQ-AI-001 through REQ-AI-005, REQ-AI-028
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The AI tutor's response message to display to the student.
    /// Always in English (REQ-CHAT-003).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of new steps completed by this message.
    /// 0 if the message didn't advance progress.
    /// For binary goals, 1 means the goal is complete.
    /// </summary>
    public int ProgressIncrement { get; set; } = 0;

    /// <summary>
    /// Whether the student's message was classified as off-topic.
    /// Only clearly unrelated messages should be flagged (REQ-AI-006).
    /// Mixed messages containing any on-topic content are classified as on-topic (REQ-AI-011).
    /// </summary>
    public bool IsOffTopic { get; set; } = false;

    /// <summary>
    /// Whether the student's message contributed to goal progress.
    /// Used for highlighting messages in the UI (REQ-CHAT-007).
    /// Pre-computed during message processing (REQ-AI-028).
    /// </summary>
    public bool ContributesToProgress { get; set; } = false;

    /// <summary>
    /// Indicates if there was an error processing the message.
    /// When true, the Message contains an error message for the student.
    /// </summary>
    public bool IsError { get; set; } = false;

    /// <summary>
    /// Creates an error response for API failures.
    /// Per REQ-AI-022: Display generic error message, no classification or progress update.
    /// </summary>
    public static ChatResponse CreateErrorResponse()
    {
        return new ChatResponse
        {
            Message = "Sorry, I'm having trouble responding right now. Please try again in a moment.",
            ProgressIncrement = 0,
            IsOffTopic = false,
            ContributesToProgress = false,
            IsError = true
        };
    }

    /// <summary>
    /// Creates a successful response from AI output.
    /// </summary>
    public static ChatResponse CreateSuccess(
        string message,
        int progressIncrement = 0,
        bool isOffTopic = false,
        bool contributesToProgress = false)
    {
        return new ChatResponse
        {
            Message = message,
            ProgressIncrement = Math.Max(0, progressIncrement), // Ensure non-negative
            IsOffTopic = isOffTopic,
            ContributesToProgress = contributesToProgress,
            IsError = false
        };
    }
}
