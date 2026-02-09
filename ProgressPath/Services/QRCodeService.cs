using QRCoder;

namespace ProgressPath.Services;

/// <summary>
/// QR code generation service using QRCoder library.
/// Uses PngByteQRCode for cross-platform compatibility (Linux Azure App Service).
/// REQ-GROUP-012
/// </summary>
public class QRCodeService : IQRCodeService
{
    /// <inheritdoc />
    public string GenerateQRCodeBase64(string joinUrl)
    {
        if (string.IsNullOrEmpty(joinUrl))
        {
            throw new ArgumentException("Join URL cannot be null or empty.", nameof(joinUrl));
        }

        using var qrGenerator = new QRCodeGenerator();

        // Generate QR code data with Q error correction level for good reliability
        var qrCodeData = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.Q);

        // Use PngByteQRCode for cross-platform compatibility (no System.Drawing dependency)
        var qrCode = new PngByteQRCode(qrCodeData);

        // Get PNG bytes with 10 pixels per module for good readability
        var pngBytes = qrCode.GetGraphic(10);

        // Convert to base64 with data URI prefix for embedding in HTML
        var base64String = Convert.ToBase64String(pngBytes);

        return $"data:image/png;base64,{base64String}";
    }
}
