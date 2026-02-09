using ProgressPath.Models;
using ProgressPath.Models.DTOs;

namespace ProgressPath.Services;

/// <summary>
/// Interface for group management operations.
/// Handles group creation, goal interpretation workflow, and group retrieval.
/// REQ-GROUP-001 through REQ-GROUP-013
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Creates a new learning group with a unique join code.
    /// Generates an AI interpretation of the goal which must be confirmed before students can join.
    /// REQ-GROUP-001, REQ-GROUP-002, REQ-GROUP-005
    /// </summary>
    /// <param name="name">Display name for the group (e.g., "A2 - quadratic equations 1").</param>
    /// <param name="goalDescription">The learning goal in English. Maximum 500 characters (REQ-GROUP-004).</param>
    /// <param name="teacherId">ID of the teacher creating the group.</param>
    /// <returns>The created group with AI goal interpretation.</returns>
    /// <exception cref="ArgumentException">Thrown if goal description exceeds 500 characters.</exception>
    /// <exception cref="LLMServiceException">Thrown if AI interpretation fails (REQ-GROUP-011).</exception>
    Task<Group> CreateGroupAsync(string name, string goalDescription, int teacherId);

    /// <summary>
    /// Retrieves the stored AI goal interpretation for a group.
    /// REQ-GROUP-006, REQ-GROUP-008
    /// </summary>
    /// <param name="groupId">ID of the group.</param>
    /// <returns>
    /// The goal interpretation containing:
    /// - Goal type (binary/percentage)
    /// - Discrete steps for percentage goals
    /// - Sample welcome message
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if group doesn't exist.</exception>
    Task<GoalInterpretation> GetGoalInterpretationAsync(int groupId);

    /// <summary>
    /// Confirms the AI's goal interpretation, making the group joinable by students.
    /// REQ-GROUP-007, REQ-GROUP-010
    /// </summary>
    /// <param name="groupId">ID of the group to confirm.</param>
    /// <exception cref="KeyNotFoundException">Thrown if group doesn't exist.</exception>
    Task ConfirmGoalInterpretationAsync(int groupId);

    /// <summary>
    /// Rejects the AI's goal interpretation and triggers a new interpretation with an updated goal.
    /// REQ-GROUP-009
    /// </summary>
    /// <param name="groupId">ID of the group to update.</param>
    /// <param name="newGoalDescription">New goal description in English. Maximum 500 characters.</param>
    /// <exception cref="KeyNotFoundException">Thrown if group doesn't exist.</exception>
    /// <exception cref="ArgumentException">Thrown if new goal description exceeds 500 characters.</exception>
    /// <exception cref="LLMServiceException">Thrown if AI interpretation fails (REQ-GROUP-011).</exception>
    Task RejectGoalInterpretationAsync(int groupId, string newGoalDescription);

    /// <summary>
    /// Finds a group by its unique join code.
    /// REQ-GROUP-012, REQ-GROUP-013
    /// </summary>
    /// <param name="joinCode">The 6-8 character alphanumeric join code (case-insensitive).</param>
    /// <returns>The group if found, null otherwise.</returns>
    Task<Group?> GetGroupByJoinCodeAsync(string joinCode);

    /// <summary>
    /// Retrieves all groups created by a specific teacher.
    /// REQ-GROUP-003
    /// </summary>
    /// <param name="teacherId">ID of the teacher.</param>
    /// <returns>All groups owned by the teacher, ordered by creation date descending.</returns>
    Task<IEnumerable<Group>> GetTeacherGroupsAsync(int teacherId);

    /// <summary>
    /// Retrieves a group by ID, only if owned by the specified teacher.
    /// Used for authorization on the teacher dashboard.
    /// REQ-DASH-004
    /// </summary>
    /// <param name="groupId">ID of the group to retrieve.</param>
    /// <param name="teacherId">ID of the teacher who must own the group.</param>
    /// <returns>The group if found and owned by the teacher, null otherwise.</returns>
    Task<Group?> GetGroupByIdAsync(int groupId, int teacherId);

    /// <summary>
    /// Generates a unique 6-8 character alphanumeric join code.
    /// REQ-GROUP-002, REQ-GROUP-013
    /// </summary>
    /// <returns>A new unique join code.</returns>
    string GenerateJoinCode();
}
