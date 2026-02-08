using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Interface for chat message handling and progress tracking operations.
/// Manages student-AI conversations and tracks goal progress.
/// REQ-CHAT-001 through REQ-CHAT-009, REQ-AI-021 through REQ-AI-028
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a student message and returns the AI tutor's response.
    /// Handles:
    /// - Storing the student message (REQ-AI-021)
    /// - Processing via LLM with full context (REQ-AI-025, REQ-AI-026)
    /// - Storing the AI response
    /// - Updating student progress (only increases, never decreases - REQ-GOAL-011)
    /// - Flagging messages for progress contribution and off-topic status (REQ-AI-028, REQ-CHAT-006)
    /// - Goal completion detection (REQ-GOAL-013)
    /// </summary>
    /// <param name="sessionId">The student session ID.</param>
    /// <param name="content">The student's message content.</param>
    /// <returns>The AI tutor's response message.</returns>
    /// <exception cref="ArgumentException">Thrown if content is empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the session is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the session's goal is already completed (REQ-GOAL-014).
    /// </exception>
    /// <remarks>
    /// On LLM API failure:
    /// - Student message is still persisted (REQ-AI-021)
    /// - Error response is returned (REQ-AI-022)
    /// - No classification or progress update (REQ-AI-023)
    /// - Inactivity timer still resets (REQ-AI-024)
    /// </remarks>
    Task<ChatMessage> SendMessageAsync(int sessionId, string content);

    /// <summary>
    /// Retrieves all chat messages for a session ordered by timestamp.
    /// Used for displaying chat history (REQ-CHAT-005).
    /// </summary>
    /// <param name="sessionId">The student session ID.</param>
    /// <returns>All messages in chronological order.</returns>
    Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int sessionId);

    /// <summary>
    /// Retrieves messages that contributed to goal progress.
    /// Returns only messages where ContributesToProgress = true.
    /// Used for teacher dashboard detail view (REQ-DASH-015).
    /// </summary>
    /// <param name="sessionId">The student session ID.</param>
    /// <returns>Progress-contributing messages in chronological order.</returns>
    Task<IEnumerable<ChatMessage>> GetProgressMessagesAsync(int sessionId);
}
