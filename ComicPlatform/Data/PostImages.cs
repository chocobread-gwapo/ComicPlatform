using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace ComicPlatform.Data;

/// <summary>
/// Everything about the images attached to creator posts, in one place: what's allowed,
/// where it's saved (wwwroot/uploads/posts), and cleaning up after it.
/// </summary>
public static class PostImages
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private const string UrlPrefix = "/uploads/posts/";

    // Files in wwwroot are served by their extension, so only plain image types are accepted.
    // SVG is left out on purpose: it can carry scripts.
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    /// <summary>Returns a message to show the person, or null if the file is fine.</summary>
    public static string? Check(IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return "Please choose a JPG, PNG, GIF or WebP image.";
        }

        if (file.Size > MaxBytes)
        {
            return $"That image is {file.Size / (1024.0 * 1024.0):F1} MB, and the limit is {MaxBytes / (1024 * 1024)} MB.";
        }

        return null;
    }

    /// <summary>Saves the file under a new random name and returns its address (e.g. /uploads/posts/abc.png).</summary>
    public static async Task<string> SaveAsync(IWebHostEnvironment env, IBrowserFile file)
    {
        var folder = Path.Combine(env.WebRootPath, "uploads", "posts");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name).ToLowerInvariant()}";

        await using var stream = file.OpenReadStream(maxAllowedSize: MaxBytes);
        await using var fileStream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return UrlPrefix + fileName;
    }

    /// <summary>Deletes an uploaded post image. Anything outside the posts folder is ignored.</summary>
    public static void Delete(IWebHostEnvironment env, string? url)
    {
        if (url is null || !url.StartsWith(UrlPrefix, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            // GetFileName drops any folder parts, so a doctored address can't point elsewhere.
            var path = Path.Combine(env.WebRootPath, "uploads", "posts", Path.GetFileName(url));
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
}
