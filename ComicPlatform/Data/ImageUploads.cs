using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace ComicPlatform.Data;

/// <summary>Thrown when an upload isn't an acceptable image. The message is safe to show to the person.</summary>
public sealed class InvalidImageException(string message) : Exception(message);

/// <summary>
/// One safe way to save an uploaded image under wwwroot/uploads/{folder}, used for covers, chapter
/// pages and post images.
///
/// Files in wwwroot are served using their extension, so the extension is the dangerous part: a file
/// called "x.html" would be served as a web page on the site's own address, and scripts in it would run
/// there. So the name the person's file had is ignored completely. The file's first bytes (its real
/// signature) decide whether it's a JPG, PNG, GIF or WebP, and the saved file gets a random name with
/// the matching extension. Anything else, SVG included, is refused.
/// </summary>
public static class ImageUploads
{
    public const string NotAnImage = "Please choose a JPG, PNG, GIF or WebP image.";

    public static async Task<string> SaveAsync(IWebHostEnvironment env, IBrowserFile file, string folder, long maxBytes)
    {
        if (file.Size > maxBytes)
        {
            throw new InvalidImageException(
                $"That image is {file.Size / (1024.0 * 1024.0):F1} MB, and the limit is {maxBytes / (1024 * 1024)} MB.");
        }

        await using var input = file.OpenReadStream(maxAllowedSize: maxBytes);

        var header = new byte[12];
        var read = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

        var extension = DetectExtension(header.AsSpan(0, read));
        if (extension is null)
        {
            throw new InvalidImageException(NotAnImage);
        }

        var directory = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{extension}";

        await using (var output = new FileStream(Path.Combine(directory, fileName), FileMode.Create))
        {
            await output.WriteAsync(header.AsMemory(0, read));
            await input.CopyToAsync(output);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    /// <summary>Deletes an image saved by SaveAsync. Anything that isn't in that folder is ignored.</summary>
    public static void Delete(IWebHostEnvironment env, string folder, string? url)
    {
        if (url is null || !url.StartsWith($"/uploads/{folder}/", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            // GetFileName drops any folder parts, so a doctored address can't point somewhere else.
            var path = Path.Combine(env.WebRootPath, "uploads", folder, Path.GetFileName(url));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A leftover file isn't worth failing the request over.
        }
    }

    private static string? DetectExtension(ReadOnlySpan<byte> h)
    {
        // JPEG: FF D8 FF
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
        {
            return ".jpg";
        }

        // PNG: 89 'P' 'N' 'G' 0D 0A 1A 0A
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        // GIF: "GIF87a" or "GIF89a"
        if (h.Length >= 6 && (h[..6].SequenceEqual("GIF87a"u8) || h[..6].SequenceEqual("GIF89a"u8)))
        {
            return ".gif";
        }

        // WebP: "RIFF", four size bytes, then "WEBP"
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
