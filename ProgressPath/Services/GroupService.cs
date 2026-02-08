using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;
using ProgressPath.Models.DTOs;

namespace ProgressPath.Services;

/// <summary>
/// Implementation of group management operations.
/// Handles group creation, goal interpretation workflow, and group retrieval.
/// REQ-GROUP-001 through REQ-GROUP-013
/// </summary>
public class GroupService : IGroupService
{
    private readonly ProgressPathDbContext _dbContext;
    private readonly ILLMService _llmService;
    private readonly ILogger<GroupService> _logger;

    /// <summary>
    /// Length of generated join codes (6 characters per REQ-GROUP-013 minimum).
    /// </summary>
    private const int JoinCodeLength = 6;

    /// <summary>
    /// Characters used for join code generation.
    /// Excludes confusing characters: 0/O, 1/I/L
    /// </summary>
    private const string JoinCodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>
    /// Maximum length for goal descriptions (REQ-GROUP-004).
    /// </summary>
    private const int MaxGoalDescriptionLength = 500;

    /// <summary>
    /// Maximum attempts to generate a unique join code before failing.
    /// </summary>
    private const int MaxJoinCodeAttempts = 10;

    /// <summary>
    /// JSON serializer options matching LLMService for consistency.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = false
    };

    public GroupService(
        ProgressPathDbContext dbContext,
        ILLMService llmService,
        ILogger<GroupService> logger)
    {
        _dbContext = dbContext;
        _llmService = llmService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Group> CreateGroupAsync(string name, string goalDescription, int teacherId)
    {
        // Validate goal description length (REQ-GROUP-004)
        if (string.IsNullOrWhiteSpace(goalDescription))
        {
            throw new ArgumentException("Goal description cannot be empty.", nameof(goalDescription));
        }

        if (goalDescription.Length > MaxGoalDescriptionLength)
        {
            throw new ArgumentException(
                $"Goal description cannot exceed {MaxGoalDescriptionLength} characters. Current length: {goalDescription.Length}.",
                nameof(goalDescription));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name cannot be empty.", nameof(name));
        }

        // Generate unique join code (REQ-GROUP-002, REQ-GROUP-013)
        string joinCode = await GenerateUniqueJoinCodeAsync();

        _logger.LogInformation(
            "Creating group '{Name}' with join code {JoinCode} for teacher {TeacherId}",
            name, joinCode, teacherId);

        // Get AI interpretation of the goal (REQ-GROUP-006)
        var interpretation = await _llmService.InterpretGoalAsync(goalDescription);

        // Create the group entity
        var group = new Group
        {
            Name = name.Trim(),
            GoalDescription = goalDescription.Trim(),
            JoinCode = joinCode,
            TeacherId = teacherId,
            GoalType = interpretation.GoalType,
            TotalSteps = interpretation.GoalType == GoalType.Percentage ? interpretation.TotalSteps : null,
            GoalInterpretation = JsonSerializer.Serialize(interpretation, JsonOptions),
            WelcomeMessage = interpretation.WelcomeMessage,
            IsConfirmed = false, // Group not joinable until teacher confirms (REQ-GROUP-010)
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Group {GroupId} created with {GoalType} goal and {Steps} steps",
            group.Id, group.GoalType, group.TotalSteps ?? 1);

        return group;
    }

    /// <inheritdoc />
    public async Task<GoalInterpretation> GetGoalInterpretationAsync(int groupId)
    {
        var group = await _dbContext.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException($"Group with ID {groupId} not found.");

        if (string.IsNullOrEmpty(group.GoalInterpretation))
        {
            _logger.LogWarning("Group {GroupId} has no stored goal interpretation", groupId);
            return GoalInterpretation.Empty;
        }

        try
        {
            var interpretation = JsonSerializer.Deserialize<GoalInterpretation>(
                group.GoalInterpretation, JsonOptions);

            return interpretation ?? GoalInterpretation.Empty;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize goal interpretation for group {GroupId}", groupId);
            return GoalInterpretation.Empty;
        }
    }

    /// <inheritdoc />
    public async Task ConfirmGoalInterpretationAsync(int groupId)
    {
        var group = await _dbContext.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException($"Group with ID {groupId} not found.");

        group.IsConfirmed = true;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Group {GroupId} goal interpretation confirmed", groupId);
    }

    /// <inheritdoc />
    public async Task RejectGoalInterpretationAsync(int groupId, string newGoalDescription)
    {
        // Validate new goal description (REQ-GROUP-004)
        if (string.IsNullOrWhiteSpace(newGoalDescription))
        {
            throw new ArgumentException("Goal description cannot be empty.", nameof(newGoalDescription));
        }

        if (newGoalDescription.Length > MaxGoalDescriptionLength)
        {
            throw new ArgumentException(
                $"Goal description cannot exceed {MaxGoalDescriptionLength} characters. Current length: {newGoalDescription.Length}.",
                nameof(newGoalDescription));
        }

        var group = await _dbContext.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException($"Group with ID {groupId} not found.");

        _logger.LogInformation(
            "Rejecting goal interpretation for group {GroupId} and reinterpreting with new goal",
            groupId);

        // Get new AI interpretation (REQ-GROUP-009)
        var interpretation = await _llmService.InterpretGoalAsync(newGoalDescription);

        // Update group with new interpretation
        group.GoalDescription = newGoalDescription.Trim();
        group.GoalType = interpretation.GoalType;
        group.TotalSteps = interpretation.GoalType == GoalType.Percentage ? interpretation.TotalSteps : null;
        group.GoalInterpretation = JsonSerializer.Serialize(interpretation, JsonOptions);
        group.WelcomeMessage = interpretation.WelcomeMessage;
        group.IsConfirmed = false; // Keep unconfirmed until teacher approves new interpretation

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Group {GroupId} reinterpreted with {GoalType} goal and {Steps} steps",
            group.Id, group.GoalType, group.TotalSteps ?? 1);
    }

    /// <inheritdoc />
    public async Task<Group?> GetGroupByJoinCodeAsync(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            return null;
        }

        // Case-insensitive comparison using ToUpper (REQ-GROUP-013)
        var normalizedCode = joinCode.Trim().ToUpperInvariant();

        return await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.JoinCode.ToUpper() == normalizedCode);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Group>> GetTeacherGroupsAsync(int teacherId)
    {
        // Include student sessions for active count (REQ-GROUP-003)
        return await _dbContext.Groups
            .Where(g => g.TeacherId == teacherId)
            .Include(g => g.StudentSessions)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Group?> GetGroupByIdAsync(int groupId, int teacherId)
    {
        var group = await _dbContext.Groups
            .Include(g => g.StudentSessions)
                .ThenInclude(s => s.HelpAlerts)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return null;
        }

        // Verify ownership (security check)
        if (group.TeacherId != teacherId)
        {
            _logger.LogWarning(
                "Unauthorized access attempt: Teacher {TeacherId} tried to access group {GroupId} owned by teacher {OwnerId}",
                teacherId, groupId, group.TeacherId);
            return null;
        }

        return group;
    }

    /// <inheritdoc />
    public string GenerateJoinCode()
    {
        // Use Random.Shared for thread safety
        var chars = new char[JoinCodeLength];
        for (int i = 0; i < JoinCodeLength; i++)
        {
            chars[i] = JoinCodeChars[Random.Shared.Next(JoinCodeChars.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// Generates a unique join code that doesn't already exist in the database.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a unique code cannot be generated after maximum attempts.
    /// </exception>
    private async Task<string> GenerateUniqueJoinCodeAsync()
    {
        for (int attempt = 0; attempt < MaxJoinCodeAttempts; attempt++)
        {
            var code = GenerateJoinCode();

            // Check if code already exists
            var existingGroup = await GetGroupByJoinCodeAsync(code);
            if (existingGroup == null)
            {
                return code;
            }

            _logger.LogDebug(
                "Join code {Code} already exists, generating new one (attempt {Attempt}/{Max})",
                code, attempt + 1, MaxJoinCodeAttempts);
        }

        throw new InvalidOperationException(
            $"Failed to generate unique join code after {MaxJoinCodeAttempts} attempts.");
    }
}
