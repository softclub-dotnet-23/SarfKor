namespace WebApi;

// Identifies the actual image format from its magic bytes, ignoring the client-supplied
// Content-Type header and filename extension (both are attacker-controlled).
public static class ImageContentTypeDetector
{
    public static async Task<string?> DetectExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";

        if (bytesRead >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return ".png";

        return null;
    }
}
