namespace ProgressPath.Services;

/// <summary>
/// Interface for QR code generation.
/// REQ-GROUP-012
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generates a QR code image for a join URL and returns it as a base64-encoded PNG data URI.
    /// The QR code encodes a URL in the format: {BaseUrl}/join/{JoinCode}
    /// </summary>
    /// <param name="joinUrl">The full URL to encode (e.g., "https://myapp.azurewebsites.net/join/ABC123").</param>
    /// <returns>Base64-encoded PNG image with data URI prefix (e.g., "data:image/png;base64,...").</returns>
    string GenerateQRCodeBase64(string joinUrl);
}
