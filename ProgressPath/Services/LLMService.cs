using System.Text.Encodings.Web;
using System.Text.Json;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Code;
using Microsoft.Extensions.Options;
using ProgressPath.Models;
using ProgressPath.Models.DTOs;

namespace ProgressPath.Services;

/// <summary>
/// LLM service implementation using LLMTornado for multi-provider AI integration.
/// REQ-LLM-001 through REQ-LLM-004
/// </summary>
public class LLMService : ILLMService
{
    private readonly TornadoApi _api;
    private readonly string _model;
    private readonly ILogger<LLMService> _logger;
    private readonly LLMSettings _settings;

    private const int MaxRetries = 3;
    private const int MaxMessagesBeforeSummarization = 50;

    // JSON serializer options with lenient parsing
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public LLMService(IOptions<LLMSettings> settings, ILogger<LLMService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!_settings.IsValid())
        {
            _logger.LogWarning(
                "LLM settings are not fully configured. Provider: {Provider}, Model: {Model}, ApiKey: {HasApiKey}",
                string.IsNullOrEmpty(_settings.Provider) ? "(empty)" : _settings.Provider,
                string.IsNullOrEmpty(_settings.Model) ? "(empty)" : _settings.Model,
                !string.IsNullOrEmpty(_settings.ApiKey));
        }

        // Initialize TornadoApi with the configured provider
        var provider = MapProviderString(_settings.Provider);
        _api = new TornadoApi(provider, _settings.ApiKey);
        _model = _settings.Model;

        _logger.LogInformation(
            "LLMService initialized with provider: {Provider}, model: {Model}",
            _settings.Provider,
            _settings.Model);
    }

    /// <inheritdoc />
    public async Task<GoalInterpretation> InterpretGoalAsync(string goalDescription)
    {
        _logger.LogDebug("Interpreting goal: {GoalDescription}", goalDescription);

        return await ExecuteWithRetryAsync(async () =>
        {
            var conversation = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = _model,
                ResponseFormat = ChatRequestResponseFormats.Json
            });

            conversation.AppendSystemMessage(PromptTemplates.GOAL_INTERPRETATION_SYSTEM_PROMPT);
            conversation.AppendUserInput($"Please analyze this learning goal and provide your interpretation:\n\n{goalDescription}");

            var response = await conversation.GetResponse();

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new LLMServiceException("Empty response from LLM API");
            }

            var interpretation = ParseGoalInterpretationResponse(response);

            // Enforce business rules
            // REQ-GOAL-004: If only 1 step, convert to binary
            if (interpretation.Steps.Count == 1)
            {
                interpretation.GoalType = GoalType.Binary;
            }

            // REQ-GOAL-005: Enforce minimum 2 steps for percentage goals
            if (interpretation.GoalType == GoalType.Percentage && interpretation.Steps.Count < 2)
            {
                interpretation.GoalType = GoalType.Binary;
            }

            // REQ-GOAL-003: If more than 10 steps, group into 10 logical stages
            if (interpretation.Steps.Count > 10)
            {
                interpretation.Steps = GroupStepsIntoStages(interpretation.Steps, 10);
            }

            _logger.LogInformation(
                "Goal interpreted as {GoalType} with {StepCount} steps",
                interpretation.GoalType,
                interpretation.Steps.Count);

            return interpretation;
        }, "Goal interpretation");
    }

    /// <inheritdoc />
    public async Task<ChatResponse> ProcessStudentMessageAsync(ChatContext context, string studentMessage)
    {
        _logger.LogDebug(
            "Processing student message. Current progress: {Progress}/{Total}, Off-topic count: {OffTopic}",
            context.CurrentProgress,
            context.TotalSteps ?? 1,
            context.OffTopicWarningCount);

        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var conversation = _api.Chat.CreateConversation(new ChatRequest
                {
                    Model = _model,
                    ResponseFormat = ChatRequestResponseFormats.Json
                });

                // Build system prompt with context
                var systemPrompt = PromptTemplates.BuildStudentGuidancePrompt(
                    context.GoalDescription,
                    context.GoalType.ToString(),
                    context.StepDescriptions,
                    context.CurrentProgress,
                    context.TotalSteps ?? 1,
                    context.OffTopicWarningCount);

                conversation.AppendSystemMessage(systemPrompt);

                // Build message history
                await BuildMessageHistoryForRequest(conversation, context);

                // Add the new student message
                conversation.AppendUserInput(studentMessage);

                var response = await conversation.GetResponse();

                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new LLMServiceException("Empty response from LLM API");
                }

                var chatResponse = ParseChatResponse(response);

                _logger.LogDebug(
                    "Chat response: OverallProgress={Progress}, IsOffTopic={OffTopic}, SignificantProgress={Significant}",
                    chatResponse.OverallProgress,
                    chatResponse.IsOffTopic,
                    chatResponse.SignificantProgress);

                return chatResponse;
            }, "Student message processing");
        }
        catch (Exception ex)
        {
            // Per REQ-AI-022: On failure, return error response instead of throwing
            _logger.LogError(ex, "Failed to process student message after retries");
            return ChatResponse.CreateErrorResponse();
        }
    }

    /// <summary>
    /// Maps a provider string to LLMTornado's LLmProviders enum.
    /// </summary>
    private static LLmProviders MapProviderString(string provider)
    {
        return provider?.ToLowerInvariant() switch
        {
            "openai" => LLmProviders.OpenAi,
            "anthropic" => LLmProviders.Anthropic,
            "google" => LLmProviders.Google,
            "groq" => LLmProviders.Groq,
            "mistral" => LLmProviders.Mistral,
            "cohere" => LLmProviders.Cohere,
            "deepseek" => LLmProviders.DeepSeek,
            "xai" => LLmProviders.XAi,
            "perplexity" => LLmProviders.Perplexity,
            "openrouter" => LLmProviders.OpenRouter,
            "deepinfra" => LLmProviders.DeepInfra,
            _ => LLmProviders.OpenAi // Default to OpenAI
        };
    }

    /// <summary>
    /// Parses the AI response for goal interpretation.
    /// </summary>
    private GoalInterpretation ParseGoalInterpretationResponse(string response)
    {
        try
        {
            // Try to extract JSON from the response (AI might include explanatory text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                throw new LLMServiceException($"No valid JSON found in response: {response}");
            }

            var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var parsed = JsonSerializer.Deserialize<GoalInterpretationJson>(jsonString, JsonOptions);

            if (parsed == null)
            {
                throw new LLMServiceException("Failed to parse goal interpretation response");
            }

            var goalType = parsed.GoalType?.ToLowerInvariant() == "percentage"
                ? GoalType.Percentage
                : GoalType.Binary;

            return new GoalInterpretation
            {
                GoalType = goalType,
                Steps = parsed.Steps ?? new List<string>(),
                WelcomeMessage = parsed.WelcomeMessage ?? "Welcome! Let's work on your learning goal together.",
                InitialGuidance = parsed.InitialGuidance ?? "Let's get started! What would you like to work on first?"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse goal interpretation JSON: {Response}", response);
            throw new LLMServiceException($"Failed to parse goal interpretation response: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses the AI response for chat processing.
    /// </summary>
    private ChatResponse ParseChatResponse(string response)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                // If no JSON found, treat the whole response as the message
                _logger.LogWarning("No JSON found in chat response, using raw response as message");
                return ChatResponse.CreateSuccess(response);
            }

            var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var parsed = JsonSerializer.Deserialize<ChatResponseJson>(jsonString, JsonOptions);

            if (parsed == null)
            {
                return ChatResponse.CreateSuccess(response);
            }

            // If the parsed Message is null or empty, the LLM returned malformed JSON
            // (e.g. bare "{}"). Throw so the retry logic can attempt again.
            if (string.IsNullOrWhiteSpace(parsed.Message))
            {
                _logger.LogWarning(
                    "LLM returned JSON with null/empty message field. Raw response: {Response}",
                    response.Length > 200 ? response[..200] + "..." : response);
                throw new LLMServiceException(
                    "LLM returned a JSON response with no message content");
            }

            return ChatResponse.CreateSuccess(
                message: parsed.Message,
                overallProgress: Math.Clamp(parsed.OverallProgress, 0, 100),
                isOffTopic: parsed.IsOffTopic,
                significantProgress: parsed.SignificantProgress);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse chat response JSON, using raw response");
            return ChatResponse.CreateSuccess(response);
        }
    }

    /// <summary>
    /// Builds the message history for the LLM request.
    /// REQ-AI-027: Handles conversation truncation if needed.
    /// AI messages are wrapped in the expected JSON format so the LLM sees consistent
    /// JSON responses in its own history (required since ResponseFormat = Json is enabled).
    /// </summary>
    private async Task BuildMessageHistoryForRequest(Conversation conversation, ChatContext context)
    {
        var messages = context.MessageHistory;

        // REQ-AI-027: If conversation is too long, summarize older messages
        if (messages.Count > MaxMessagesBeforeSummarization)
        {
            _logger.LogInformation(
                "Conversation has {Count} messages, summarizing older messages",
                messages.Count);

            // Keep the first few messages and the last ~40
            var messagesToSummarize = messages.Take(messages.Count - 40).ToList();
            var recentMessages = messages.Skip(messages.Count - 40).ToList();

            // Summarize older messages
            var summary = await SummarizeConversation(messagesToSummarize);

            // Add summary as a system context
            conversation.AppendSystemMessage($"[Previous conversation summary: {summary}]");

            // Add recent messages
            foreach (var msg in recentMessages)
            {
                if (msg.IsFromStudent)
                {
                    conversation.AppendUserInput(msg.Content);
                }
                else
                {
                    conversation.AppendExampleChatbotOutput(WrapAsJsonResponse(msg.Content));
                }
            }
        }
        else
        {
            // Add all messages to conversation
            foreach (var msg in messages)
            {
                if (msg.IsFromStudent)
                {
                    conversation.AppendUserInput(msg.Content);
                }
                else
                {
                    conversation.AppendExampleChatbotOutput(WrapAsJsonResponse(msg.Content));
                }
            }
        }
    }

    /// <summary>
    /// JSON serializer options for wrapping AI messages in history.
    /// Uses UnsafeRelaxedJsonEscaping to avoid escaping apostrophes and other common
    /// characters (e.g. ' → \u0027), which would confuse the LLM into mimicking the escapes.
    /// </summary>
    private static readonly JsonSerializerOptions HistoryJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Wraps a plain-text AI message in the JSON response format the LLM expects.
    /// This ensures the LLM sees consistent JSON in its own history when ResponseFormat = Json is enabled,
    /// preventing it from returning bare {} responses due to format confusion.
    /// </summary>
    private static string WrapAsJsonResponse(string message)
    {
        return JsonSerializer.Serialize(new
        {
            message,
            overallProgress = 0,
            isOffTopic = false,
            significantProgress = false
        }, HistoryJsonOptions);
    }

    /// <summary>
    /// Summarizes a portion of the conversation for context preservation.
    /// REQ-AI-027
    /// </summary>
    private async Task<string> SummarizeConversation(List<ChatHistoryMessage> messages)
    {
        try
        {
            var conversation = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = _model
            });

            conversation.AppendSystemMessage(PromptTemplates.CONVERSATION_SUMMARY_PROMPT);

            var conversationText = string.Join("\n",
                messages.Select(m => $"{(m.IsFromStudent ? "Student" : "Tutor")}: {m.Content}"));

            conversation.AppendUserInput(conversationText);

            var summary = await conversation.GetResponse();
            return summary ?? "Previous conversation covered multiple topics related to the learning goal.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to summarize conversation, using fallback");
            return "Previous conversation covered multiple topics related to the learning goal.";
        }
    }

    /// <summary>
    /// Groups steps into a smaller number of logical stages.
    /// </summary>
    private static List<string> GroupStepsIntoStages(List<string> steps, int maxStages)
    {
        if (steps.Count <= maxStages)
        {
            return steps;
        }

        var stepsPerStage = (int)Math.Ceiling((double)steps.Count / maxStages);
        var stages = new List<string>();

        for (int i = 0; i < maxStages; i++)
        {
            var stageSteps = steps.Skip(i * stepsPerStage).Take(stepsPerStage).ToList();
            if (stageSteps.Count > 0)
            {
                var stageName = $"Stage {i + 1}: {string.Join("; ", stageSteps)}";
                stages.Add(stageName);
            }
        }

        return stages;
    }

    /// <summary>
    /// Executes an action with retry logic and exponential backoff.
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string operationName)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "{Operation} failed on attempt {Attempt}/{MaxRetries}",
                    operationName,
                    attempt,
                    MaxRetries);

                if (attempt < MaxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    _logger.LogDebug("Waiting {Delay}s before retry", delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }
        }

        throw new LLMServiceException(
            $"{operationName} failed after {MaxRetries} attempts",
            lastException!);
    }

    #region JSON DTOs for parsing

    private class GoalInterpretationJson
    {
        public string? GoalType { get; set; }
        public List<string>? Steps { get; set; }
        public string? WelcomeMessage { get; set; }
        public string? InitialGuidance { get; set; }
    }

    private class ChatResponseJson
    {
        public string? Message { get; set; }
        public int OverallProgress { get; set; }
        public bool IsOffTopic { get; set; }
        public bool SignificantProgress { get; set; }
    }

    #endregion
}
