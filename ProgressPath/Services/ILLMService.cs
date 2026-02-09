using ProgressPath.Models.DTOs;

namespace ProgressPath.Services;

/// <summary>
/// Interface for AI-powered LLM operations.
/// Uses LLMTornado for multi-provider abstraction (REQ-LLM-002).
/// Provider and model are configurable via application settings (REQ-LLM-001, REQ-LLM-004).
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Analyzes a teacher's goal description and returns a structured interpretation.
    /// Determines goal type (binary/percentage) and discrete steps (REQ-GROUP-006).
    /// The interpretation includes a welcome message for students (REQ-GROUP-008).
    /// </summary>
    /// <param name="goalDescription">The teacher's goal description in English (max 500 chars).</param>
    /// <returns>
    /// A GoalInterpretation containing:
    /// - GoalType: Binary or Percentage
    /// - Steps: List of step descriptions (1 for binary, 2-10 for percentage per REQ-GOAL-003)
    /// - WelcomeMessage: Message to show students when they join
    /// </returns>
    /// <exception cref="LLMServiceException">
    /// Thrown when the API fails after retry attempts.
    /// Per REQ-GROUP-011, caller should display a generic error allowing retry.
    /// </exception>
    Task<GoalInterpretation> InterpretGoalAsync(string goalDescription);

    /// <summary>
    /// Processes a student's message and generates an AI tutor response.
    /// Analyzes the message for progress contribution and on-topic/off-topic status.
    /// </summary>
    /// <param name="context">
    /// The full conversation context including:
    /// - Goal definition and type
    /// - Current progress state
    /// - Off-topic warning counter
    /// - Message history (REQ-AI-025, REQ-AI-026)
    /// </param>
    /// <param name="studentMessage">The student's new message to process.</param>
    /// <returns>
    /// A ChatResponse containing:
    /// - Message: AI tutor's response in English (REQ-CHAT-003)
    /// - OverallProgress: Student's new total progress percentage 0-100 (bias toward giving credit per REQ-AI-004)
    /// - IsOffTopic: True only for clearly unrelated messages (REQ-AI-006)
    /// - SignificantProgress: Flag for major milestones worth highlighting to teacher (REQ-AI-028)
    /// </returns>
    /// <remarks>
    /// The AI tutor:
    /// - Guides students without giving direct answers (REQ-AI-002)
    /// - Recognizes demonstrated understanding (REQ-AI-003, REQ-AI-005)
    /// - Biases toward on-topic classification when ambiguous (REQ-AI-006, REQ-AI-007)
    ///
    /// On API failure, returns an error response per REQ-AI-022 (no exception thrown).
    /// </remarks>
    Task<ChatResponse> ProcessStudentMessageAsync(ChatContext context, string studentMessage);
}

/// <summary>
/// Exception thrown when LLM service operations fail after retry attempts.
/// </summary>
public class LLMServiceException : Exception
{
    /// <summary>
    /// Creates a new LLMServiceException.
    /// </summary>
    public LLMServiceException(string message) : base(message) { }

    /// <summary>
    /// Creates a new LLMServiceException with an inner exception.
    /// </summary>
    public LLMServiceException(string message, Exception innerException)
        : base(message, innerException) { }
}
