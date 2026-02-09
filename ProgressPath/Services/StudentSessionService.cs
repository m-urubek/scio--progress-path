using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Implementation of student session management operations.
/// Handles student joining groups, session restoration, and progress tracking.
/// REQ-GROUP-014 through REQ-GROUP-024
/// </summary>
public class StudentSessionService : IStudentSessionService
{
    private readonly ProgressPathDbContext _dbContext;
    private readonly ILogger<StudentSessionService> _logger;

    public StudentSessionService(
        ProgressPathDbContext dbContext,
        ILogger<StudentSessionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StudentSession> JoinGroupAsync(string joinCode, string nickname, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            throw new ArgumentException("Join code cannot be empty.", nameof(joinCode));
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Nickname cannot be empty.", nameof(nickname));
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(deviceId));
        }

        // Normalize join code for case-insensitive comparison
        var normalizedCode = joinCode.Trim().ToUpperInvariant();
        var trimmedNickname = nickname.Trim();
        var trimmedDeviceId = deviceId.Trim();

        // Find the group by join code
        var group = await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.JoinCode.ToUpper() == normalizedCode);

        if (group == null)
        {
            _logger.LogWarning("Attempted to join non-existent group with code {JoinCode}", normalizedCode);
            throw new KeyNotFoundException($"Group with join code '{joinCode}' not found.");
        }

        // Check if group is confirmed (REQ-GROUP-010)
        if (!group.IsConfirmed)
        {
            _logger.LogWarning("Attempted to join unconfirmed group {GroupId}", group.Id);
            throw new InvalidOperationException("Group is not yet available for joining.");
        }

        // Check for existing session with same device (session restoration - REQ-GROUP-018)
        var existingSession = await _dbContext.StudentSessions
            .Include(s => s.Group)
            .Include(s => s.HelpAlerts)
            .FirstOrDefaultAsync(s => s.GroupId == group.Id && s.DeviceId == trimmedDeviceId);

        if (existingSession != null)
        {
            _logger.LogInformation(
                "Restored existing session {SessionId} for device {DeviceId} in group {GroupId}",
                existingSession.Id, trimmedDeviceId, group.Id);
            return existingSession;
        }

        // Check nickname uniqueness (case-insensitive) - REQ-GROUP-016
        var normalizedNickname = trimmedNickname.ToUpperInvariant();
        var nicknameExists = await _dbContext.StudentSessions
            .AnyAsync(s => s.GroupId == group.Id && s.Nickname.ToUpper() == normalizedNickname);

        if (nicknameExists)
        {
            _logger.LogWarning(
                "Nickname '{Nickname}' already taken in group {GroupId}",
                trimmedNickname, group.Id);
            throw new ArgumentException($"Nickname '{trimmedNickname}' is already taken in this group. Please choose a different nickname.");
        }

        // Create new session
        var session = new StudentSession
        {
            GroupId = group.Id,
            Nickname = trimmedNickname,
            DeviceId = trimmedDeviceId,
            JoinedAt = DateTime.UtcNow,
            CurrentProgress = 0,
            IsCompleted = false,
            OffTopicWarningCount = 0,
            HasActiveAlert = false,
            AlertType = null,
            LastActivityAt = null
        };

        try
        {
            _dbContext.StudentSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            // Load the Group navigation property for the caller
            session.Group = group;

            _logger.LogInformation(
                "Student '{Nickname}' joined group {GroupId} with session {SessionId}",
                trimmedNickname, group.Id, session.Id);

            return session;
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            // Handle race condition where device or nickname was inserted by another request
            _logger.LogWarning(
                ex,
                "Duplicate key conflict when creating session for device {DeviceId} or nickname {Nickname} in group {GroupId}",
                trimmedDeviceId, trimmedNickname, group.Id);

            // Check if it's a device duplicate - restore existing session
            existingSession = await _dbContext.StudentSessions
                .Include(s => s.Group)
                .Include(s => s.HelpAlerts)
                .FirstOrDefaultAsync(s => s.GroupId == group.Id && s.DeviceId == trimmedDeviceId);

            if (existingSession != null)
            {
                return existingSession;
            }

            // Otherwise it's a nickname duplicate
            throw new ArgumentException($"Nickname '{trimmedNickname}' is already taken in this group. Please choose a different nickname.");
        }
    }

    /// <inheritdoc />
    public async Task<StudentSession?> GetExistingSessionAsync(string joinCode, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(joinCode) || string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        // Normalize join code for case-insensitive comparison
        var normalizedCode = joinCode.Trim().ToUpperInvariant();
        var trimmedDeviceId = deviceId.Trim();

        // Find the group first
        var group = await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.JoinCode.ToUpper() == normalizedCode);

        if (group == null)
        {
            return null;
        }

        // Find existing session for this device in the group
        return await _dbContext.StudentSessions
            .Include(s => s.Group)
            .Include(s => s.HelpAlerts)
            .FirstOrDefaultAsync(s => s.GroupId == group.Id && s.DeviceId == trimmedDeviceId);
    }

    /// <inheritdoc />
    public async Task<bool> IsNicknameAvailableAsync(int groupId, string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return false;
        }

        var normalizedNickname = nickname.Trim().ToUpperInvariant();

        var exists = await _dbContext.StudentSessions
            .AnyAsync(s => s.GroupId == groupId && s.Nickname.ToUpper() == normalizedNickname);

        return !exists;
    }

    /// <inheritdoc />
    public async Task UpdateProgressAsync(int sessionId, int newProgress)
    {
        var session = await _dbContext.StudentSessions
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException($"Student session with ID {sessionId} not found.");
        }

        // Progress can only increase (REQ-GOAL-011)
        if (newProgress <= session.CurrentProgress)
        {
            _logger.LogDebug(
                "Ignoring progress update for session {SessionId}: new progress {NewProgress} <= current {CurrentProgress}",
                sessionId, newProgress, session.CurrentProgress);
            return;
        }

        var oldProgress = session.CurrentProgress;
        session.CurrentProgress = newProgress;

        // Check for goal completion
        bool wasCompleted = session.IsCompleted;

        if (session.Group.GoalType == GoalType.Binary)
        {
            // Binary goal: completed when progress >= 1
            if (newProgress >= 1)
            {
                session.IsCompleted = true;
            }
        }
        else // Percentage goal
        {
            // Percentage goal: completed when progress >= 100%
            if (newProgress >= 100)
            {
                session.IsCompleted = true;
            }
        }

        await _dbContext.SaveChangesAsync();

        if (!wasCompleted && session.IsCompleted)
        {
            _logger.LogInformation(
                "Student session {SessionId} completed goal. Progress: {Progress}/{TotalSteps}",
                sessionId, newProgress, session.Group.TotalSteps ?? 1);
        }
        else
        {
            _logger.LogInformation(
                "Updated progress for session {SessionId}: {OldProgress} -> {NewProgress}",
                sessionId, oldProgress, newProgress);
        }
    }

    /// <inheritdoc />
    public async Task UpdateLastActivityAsync(int sessionId)
    {
        var session = await _dbContext.StudentSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException($"Student session with ID {sessionId} not found.");
        }

        // Reset inactivity warning when student resumes activity
        // This ensures the two-step escalation resets for the next inactivity period
        var wasInactive = session.InactivityWarningSentAt != null;
        
        session.LastActivityAt = DateTime.UtcNow;
        session.InactivityWarningSentAt = null; // Reset warning for next inactivity cycle
        
        await _dbContext.SaveChangesAsync();

        if (wasInactive)
        {
            _logger.LogInformation(
                "Session {SessionId} resumed activity, inactivity warning reset", 
                sessionId);
        }
        else
        {
            _logger.LogDebug("Updated last activity for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StudentSession>> GetGroupSessionsAsync(int groupId)
    {
        return await _dbContext.StudentSessions
            .Where(s => s.GroupId == groupId)
            .Include(s => s.HelpAlerts)
            .OrderByDescending(s => s.JoinedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Determines if a DbUpdateException is caused by a duplicate key violation.
    /// </summary>
    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // SQL Server duplicate key error codes
        // 2601 = Cannot insert duplicate key row in object with unique index
        // 2627 = Violation of UNIQUE KEY constraint
        var innerException = ex.InnerException;
        if (innerException != null)
        {
            var message = innerException.Message;
            return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
