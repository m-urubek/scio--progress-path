using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProgressPath.Data;
using ProgressPath.Models;

namespace ProgressPath.Services;

/// <summary>
/// Interface for authentication-related operations.
/// Handles user creation and profile management from Google OAuth claims.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets an existing user by their Google ID or creates a new user if one doesn't exist.
    /// New users are assigned the Teacher role by default (REQ-AUTH-002).
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal containing Google OAuth claims.</param>
    /// <returns>The user record, or null if the principal doesn't have required claims.</returns>
    Task<User?> GetOrCreateUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the current user based on their authentication claims.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal containing Google OAuth claims.</param>
    /// <returns>The user record, or null if not found or claims are invalid.</returns>
    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Updates a user's profile data from Google OAuth claims.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="principal">The ClaimsPrincipal containing updated Google OAuth claims.</param>
    Task UpdateUserFromClaimsAsync(User user, ClaimsPrincipal principal);
}

/// <summary>
/// Implementation of IAuthenticationService for Google OAuth authentication.
/// Extracts user profile data from Google OAuth claims and manages user records.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ProgressPathDbContext _dbContext;

    public AuthenticationService(ProgressPathDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<User?> GetOrCreateUserAsync(ClaimsPrincipal principal)
    {
        var googleId = GetGoogleId(principal);
        if (string.IsNullOrEmpty(googleId))
        {
            return null;
        }

        // Try to find existing user by GoogleId
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

        if (user == null)
        {
            // Create new user with Teacher role (REQ-AUTH-002)
            user = new User
            {
                GoogleId = googleId,
                Email = GetEmail(principal) ?? string.Empty,
                DisplayName = GetDisplayName(principal) ?? string.Empty,
                ProfilePictureUrl = GetProfilePictureUrl(principal),
                Role = UserRole.Teacher,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Update existing user's profile data from fresh claims (REQ-AUTH-003)
            await UpdateUserFromClaimsAsync(user, principal);
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var googleId = GetGoogleId(principal);
        if (string.IsNullOrEmpty(googleId))
        {
            return null;
        }

        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    /// <inheritdoc />
    public async Task UpdateUserFromClaimsAsync(User user, ClaimsPrincipal principal)
    {
        var email = GetEmail(principal);
        var displayName = GetDisplayName(principal);
        var profilePictureUrl = GetProfilePictureUrl(principal);

        bool hasChanges = false;

        if (!string.IsNullOrEmpty(email) && email != user.Email)
        {
            user.Email = email;
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(displayName) && displayName != user.DisplayName)
        {
            user.DisplayName = displayName;
            hasChanges = true;
        }

        if (profilePictureUrl != user.ProfilePictureUrl)
        {
            user.ProfilePictureUrl = profilePictureUrl;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Extracts the Google ID from the NameIdentifier claim.
    /// </summary>
    private static string? GetGoogleId(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Extracts the email address from the Email claim.
    /// </summary>
    private static string? GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Extracts the display name from the Name claim.
    /// </summary>
    private static string? GetDisplayName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Extracts the profile picture URL from the 'picture' claim.
    /// Google OAuth returns the profile picture URL in this custom claim.
    /// </summary>
    private static string? GetProfilePictureUrl(ClaimsPrincipal principal)
    {
        return principal.FindFirst("picture")?.Value;
    }
}
