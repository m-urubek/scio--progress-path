using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;
using ProgressPath.Models.DTOs;

namespace ProgressPath.Services;

/// <summary>
/// Implementation of chat message handling and progress tracking operations.
/// Manages student-AI conversations and tracks goal progress.
/// REQ-CHAT-001 through REQ-CHAT-009, REQ-AI-021 through REQ-AI-028
/// </summary>
public class ChatService : IChatService
{
    private readonly ProgressPathDbContext _dbContext;
    private readonly ILLMService _llmService;
    private readonly IStudentSessionService _sessionService;
    private readonly IHubNotificationService _hubNotificationService;
    private readonly ILogger<ChatService> _logger;

    /// <summary>
    /// JSON serializer options matching GroupService for consistency.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public ChatService(
        ProgressPathDbContext dbContext,
        ILLMService llmService,
        IStudentSessionService sessionService,
        IHubNotificationService hubNotificationService,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _llmService = llmService;
        _sessionService = sessionService;
        _hubNotificationService = hubNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int sessionId)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.StudentSessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> GetProgressMessagesAsync(int sessionId)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.StudentSessionId == sessionId && m.SignificantProgress)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ChatMessage> SendMessageAsync(int sessionId, string content)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        // Load session with Group and ChatMessages for context
        var session = await _dbContext.StudentSessions
            .Include(s => s.Group)
            .Include(s => s.ChatMessages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException($"Student session with ID {sessionId} not found.");
        }

        // Check if goal is already completed (REQ-GOAL-014)
        if (session.IsCompleted)
        {
            throw new InvalidOperationException(
                "Cannot send messages after goal completion. Chat is disabled.");
        }

        var trimmedContent = content.Trim();

        // Create and save student message (REQ-AI-021: persist even if LLM fails)
        var studentMessage = new ChatMessage
        {
            StudentSessionId = session.Id,
            Content = trimmedContent,
            IsFromStudent = true,
            SignificantProgress = false,
            IsOffTopic = false,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.ChatMessages.Add(studentMessage);
        await _dbContext.SaveChangesAsync();

        // Broadcast student message to all tabs for this session (REQ-GROUP-020: multi-tab sync)
        await _hubNotificationService.NotifyNewMessageAsync(sessionId, session.GroupId, studentMessage);

        _logger.LogDebug(
            "Student message saved for session {SessionId}: {ContentPreview}",
            sessionId,
            trimmedContent.Length > 50 ? trimmedContent[..50] + "..." : trimmedContent);

        // Update last activity timestamp (REQ-AI-024: reset timer regardless of API success)
        await _sessionService.UpdateLastActivityAsync(sessionId);

        // Build chat context for LLM
        var stepDescriptions = GetStepDescriptionsFromGroup(session.Group);

        // Reload chat messages to include the one we just saved
        session.ChatMessages = await _dbContext.ChatMessages
            .Where(m => m.StudentSessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        var context = ChatContext.FromStudentSession(session, stepDescriptions);

        // Process with LLM
        var response = await _llmService.ProcessStudentMessageAsync(context, trimmedContent);

        // Process AI response and update state
        var aiMessage = await ProcessAIResponseAsync(session, studentMessage, response);

        return aiMessage;
    }

    /// <summary>
    /// Extracts step descriptions from the group's GoalInterpretation JSON.
    /// </summary>
    /// <param name="group">The group containing the goal interpretation.</param>
    /// <returns>List of step descriptions, or empty list on failure.</returns>
    private List<string> GetStepDescriptionsFromGroup(Group group)
    {
        if (string.IsNullOrEmpty(group.GoalInterpretation))
        {
            return new List<string>();
        }

        try
        {
            var interpretation = JsonSerializer.Deserialize<GoalInterpretation>(
                group.GoalInterpretation, JsonOptions);

            return interpretation?.Steps ?? new List<string>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize goal interpretation for group {GroupId}",
                group.Id);
            return new List<string>();
        }
    }

    /// <summary>
    /// Processes the AI response: creates AI message, updates progress, handles off-topic flags.
    /// </summary>
    /// <param name="session">The student session (with loaded Group).</param>
    /// <param name="studentMessage">The student's message that triggered this response.</param>
    /// <param name="response">The AI's response from LLMService.</param>
    /// <returns>The created AI ChatMessage.</returns>
    private async Task<ChatMessage> ProcessAIResponseAsync(
        StudentSession session,
        ChatMessage studentMessage,
        ChatResponse response)
    {
        // Create AI message entity
        var aiMessage = new ChatMessage
        {
            StudentSessionId = session.Id,
            Content = response.Message,
            IsFromStudent = false,
            SignificantProgress = false, // AI messages are not flagged as significant
            IsOffTopic = false,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.ChatMessages.Add(aiMessage);

        // Handle error response (REQ-AI-022, REQ-AI-023)
        if (response.IsError)
        {
            await _dbContext.SaveChangesAsync();

            // Broadcast AI error message to all tabs (REQ-GROUP-020: multi-tab sync)
            await _hubNotificationService.NotifyNewMessageAsync(session.Id, session.GroupId, aiMessage);

            _logger.LogWarning(
                "LLM API error for session {SessionId}. Error message stored.",
                session.Id);
            return aiMessage;
        }

        // Update student message flags based on AI classification
        studentMessage.IsOffTopic = response.IsOffTopic;
        studentMessage.SignificantProgress = response.SignificantProgress;

        // Handle off-topic logic (REQ-AI-008, REQ-AI-010, REQ-AI-019)
        if (response.IsOffTopic)
        {
            session.OffTopicWarningCount++;

            _logger.LogInformation(
                "Off-topic message from session {SessionId}. Warning count: {Count}",
                session.Id,
                session.OffTopicWarningCount);

            // REQ-AI-008: First offense = warning only, subsequent = escalate to alert
            if (session.OffTopicWarningCount > 1)
            {
                // Check if there's already an active off-topic alert
                var hasActiveOffTopicAlert = await _dbContext.HelpAlerts
                    .AnyAsync(a => a.StudentSessionId == session.Id
                        && a.AlertType == AlertType.OffTopic
                        && !a.IsResolved);

                if (!hasActiveOffTopicAlert)
                {
                    // Create off-topic alert for teacher
                    var alert = new HelpAlert
                    {
                        StudentSessionId = session.Id,
                        AlertType = AlertType.OffTopic,
                        IsResolved = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.HelpAlerts.Add(alert);
                    session.HasActiveAlert = true;
                    session.AlertType = AlertType.OffTopic;

                    // Save to get the alert ID before notifying
                    await _dbContext.SaveChangesAsync();

                    // Set navigation property for nickname in notification payload
                    alert.StudentSession = session;

                    // Notify teacher via SignalR and in-process events
                    await _hubNotificationService.NotifyAlertAsync(session.GroupId, alert);

                    _logger.LogInformation(
                        "Created off-topic alert for session {SessionId}",
                        session.Id);
                }
            }
        }
        else if (session.OffTopicWarningCount > 0)
        {
            // REQ-AI-010: Reset warning count when student returns to on-topic
            _logger.LogDebug(
                "Session {SessionId} returned to on-topic. Resetting warning count from {Count}",
                session.Id,
                session.OffTopicWarningCount);
            session.OffTopicWarningCount = 0;
        }

        // Handle progress update (REQ-GOAL-011: progress only increases)
        if (response.OverallProgress > session.CurrentProgress)
        {
            var newProgress = response.OverallProgress;

            _logger.LogInformation(
                "Progress update for session {SessionId}: {Old} -> {New}",
                session.Id,
                session.CurrentProgress,
                newProgress);

            // Save current changes first before calling UpdateProgressAsync
            await _dbContext.SaveChangesAsync();

            // Broadcast AI message to all tabs (REQ-GROUP-020: multi-tab sync)
            await _hubNotificationService.NotifyNewMessageAsync(session.Id, session.GroupId, aiMessage);

            // Update progress via StudentSessionService (handles never-decrease logic and completion)
            await _sessionService.UpdateProgressAsync(session.Id, newProgress);

            // Reload session to check completion status and get updated progress
            var updatedSession = await _dbContext.StudentSessions
                .AsNoTracking()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            if (updatedSession != null)
            {
                // Broadcast progress update to all tabs (REQ-GROUP-020: multi-tab sync)
                await _hubNotificationService.NotifyProgressUpdateAsync(
                    session.Id,
                    session.GroupId,
                    updatedSession.CurrentProgress,
                    updatedSession.Group?.TotalSteps ?? 1,
                    updatedSession.IsCompleted);

                if (updatedSession.IsCompleted)
                {
                    _logger.LogInformation(
                        "Goal completed for session {SessionId} (student: {Nickname})",
                        session.Id,
                        session.Nickname);
                }
            }
        }
        else
        {
            // Save changes if we didn't already save above
            await _dbContext.SaveChangesAsync();

            // Broadcast AI message to all tabs (REQ-GROUP-020: multi-tab sync)
            await _hubNotificationService.NotifyNewMessageAsync(session.Id, session.GroupId, aiMessage);
        }

        return aiMessage;
    }

    /// <inheritdoc />
    public async Task<ChatMessage?> CreateInitialGuidanceMessageAsync(int sessionId)
    {
        // Load session with Group
        var session = await _dbContext.StudentSessions
            .Include(s => s.Group)
            .Include(s => s.ChatMessages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            _logger.LogWarning("Cannot create initial guidance: session {SessionId} not found", sessionId);
            return null;
        }

        // Only create if no messages exist yet
        if (session.ChatMessages.Any())
        {
            _logger.LogDebug("Session {SessionId} already has messages, skipping initial guidance", sessionId);
            return null;
        }

        // Get the initial guidance from the group
        var initialGuidance = session.Group.InitialGuidance;
        if (string.IsNullOrWhiteSpace(initialGuidance))
        {
            // Fallback to welcome message if no initial guidance is set
            initialGuidance = session.Group.WelcomeMessage ?? "Welcome! Let's get started with your learning goal.";
        }

        // Create the initial AI message
        var aiMessage = new ChatMessage
        {
            StudentSessionId = sessionId,
            Content = initialGuidance,
            IsFromStudent = false,
            IsOffTopic = false,
            SignificantProgress = false,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.ChatMessages.Add(aiMessage);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created initial guidance message for session {SessionId}",
            sessionId);

        // Broadcast to SignalR so other tabs see it
        await _hubNotificationService.NotifyNewMessageAsync(sessionId, session.GroupId, aiMessage);

        return aiMessage;
    }
}
