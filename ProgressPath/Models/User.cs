using System.ComponentModel.DataAnnotations;

namespace ProgressPath.Models;

/// <summary>
/// Represents a teacher user in the system.
/// Users authenticate via Google OAuth and can create and manage groups.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User's email address from Google OAuth.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name from Google OAuth.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// URL to the user's profile picture from Google OAuth.
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// User's role in the system. Currently only Teacher is supported.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Teacher;

    /// <summary>
    /// Unique identifier from Google OAuth (subject claim).
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string GoogleId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Groups created by this teacher.
    /// </summary>
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}
