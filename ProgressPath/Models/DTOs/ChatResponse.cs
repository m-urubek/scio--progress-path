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
    /// The student's new overall progress percentage (0-100).
    /// Returned by the AI as an absolute value, not an increment.
    /// The system enforces that progress never decreases.
    /// For binary goals, 0 = incomplete, 1 = complete.
    /// </summary>
    public int OverallProgress { get; set; } = 0;

    /// <summary>
    /// Whether the student's message was classified as off-topic.
    /// Only clearly unrelated messages should be flagged (REQ-AI-006).
    /// Mixed messages containing any on-topic content are classified as on-topic (REQ-AI-011).
    /// </summary>
    public bool IsOffTopic { get; set; } = false;

    /// <summary>
    /// Whether this message represents a significant milestone worth highlighting to the teacher.
    /// Only major events like completing a task or demonstrating mastery should be flagged.
    /// Controls what appears in the teacher's "Key Progress Messages" list.
    /// </summary>
    public bool SignificantProgress { get; set; } = false;

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
            OverallProgress = 0,
            IsOffTopic = false,
            SignificantProgress = false,
            IsError = true
        };
    }

    /// <summary>
    /// Creates a successful response from AI output.
    /// </summary>
    public static ChatResponse CreateSuccess(
        string message,
        int overallProgress = 0,
        bool isOffTopic = false,
        bool significantProgress = false)
    {
        return new ChatResponse
        {
            Message = message,
            OverallProgress = Math.Clamp(overallProgress, 0, 100),
            IsOffTopic = isOffTopic,
            SignificantProgress = significantProgress,
            IsError = false
        };
    }
}
