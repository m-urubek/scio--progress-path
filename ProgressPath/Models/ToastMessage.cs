namespace ProgressPath.Models;

/// <summary>
/// Represents a toast notification message displayed on the teacher dashboard.
/// Used for new alerts (REQ-DASH-013) and activity resumption (REQ-DASH-012).
/// </summary>
public class ToastMessage
{
    /// <summary>
    /// Unique identifier for the toast.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The message to display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of toast: 'alert' for new alerts, 'resumed' for activity resumption.
    /// </summary>
    public string Type { get; set; } = "alert";

    /// <summary>
    /// The student's nickname (optional).
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// When the toast was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
