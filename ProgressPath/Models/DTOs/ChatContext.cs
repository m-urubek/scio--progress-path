namespace ProgressPath.Models.DTOs;

/// <summary>
/// Represents the context needed for LLM to process a student message.
/// Contains all information required for accurate progress tracking and response generation.
/// REQ-AI-025, REQ-AI-026
/// </summary>
public class ChatContext
{
    /// <summary>
    /// The teacher's original goal description.
    /// </summary>
    public string GoalDescription { get; set; } = string.Empty;

    /// <summary>
    /// The type of goal (Binary or Percentage).
    /// </summary>
    public GoalType GoalType { get; set; }

    /// <summary>
    /// Total number of steps for percentage goals.
    /// Null for binary goals.
    /// </summary>
    public int? TotalSteps { get; set; }

    /// <summary>
    /// Descriptions of each step for percentage goals.
    /// </summary>
    public List<string> StepDescriptions { get; set; } = new();

    /// <summary>
    /// Number of steps the student has completed.
    /// For binary goals: 0 = incomplete, 1 = complete.
    /// </summary>
    public int CurrentProgress { get; set; }

    /// <summary>
    /// Current count of consecutive off-topic warnings.
    /// First off-topic = warning, subsequent = escalation (REQ-AI-008).
    /// Resets when student returns to on-topic (REQ-AI-010).
    /// </summary>
    public int OffTopicWarningCount { get; set; }

    /// <summary>
    /// The conversation history for context.
    /// </summary>
    public List<ChatHistoryMessage> MessageHistory { get; set; } = new();

    /// <summary>
    /// Builds a ChatContext from a StudentSession entity.
    /// </summary>
    /// <param name="session">The student session with loaded Group and ChatMessages.</param>
    /// <param name="stepDescriptions">The parsed step descriptions from the group's goal interpretation.</param>
    /// <returns>A populated ChatContext ready for LLM processing.</returns>
    public static ChatContext FromStudentSession(StudentSession session, List<string>? stepDescriptions = null)
    {
        return new ChatContext
        {
            GoalDescription = session.Group.GoalDescription,
            GoalType = session.Group.GoalType,
            TotalSteps = session.Group.TotalSteps,
            StepDescriptions = stepDescriptions ?? new List<string>(),
            CurrentProgress = session.CurrentProgress,
            OffTopicWarningCount = session.OffTopicWarningCount,
            MessageHistory = session.ChatMessages
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatHistoryMessage
                {
                    Content = m.Content,
                    IsFromStudent = m.IsFromStudent,
                    Timestamp = m.Timestamp
                })
                .ToList()
        };
    }

    /// <summary>
    /// Gets the progress percentage for display.
    /// </summary>
    public int ProgressPercentage
    {
        get
        {
            if (GoalType == GoalType.Binary)
            {
                return CurrentProgress >= 1 ? 100 : 0;
            }

            // For percentage goals, CurrentProgress already stores the percentage (0-100)
            return Math.Clamp(CurrentProgress, 0, 100);
        }
    }
}

/// <summary>
/// Represents a single message in the conversation history.
/// </summary>
public class ChatHistoryMessage
{
    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// True if the message was sent by the student, false if from AI.
    /// </summary>
    public bool IsFromStudent { get; set; }

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
