namespace ProgressPath.Services;

/// <summary>
/// Configuration settings for the LLM service.
/// Bound to the "LLM" configuration section.
/// REQ-LLM-001, REQ-LLM-004
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "LLM";

    /// <summary>
    /// The LLM provider name (e.g., "openai", "anthropic", "google", "groq", "mistral").
    /// Maps to LLMTornado's LLmProviders enum.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The model name to use (e.g., "gpt-4", "claude-3-sonnet", "gemini-pro").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The API key for the configured provider.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Validates that required settings are configured.
    /// </summary>
    /// <returns>True if all required settings are present.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Provider)
            && !string.IsNullOrWhiteSpace(Model)
            && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
