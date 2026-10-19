using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace ComicPlatform.Data;

/// <summary>
/// Creator post images: 10 MB, saved in wwwroot/uploads/posts. The actual file checking and saving
/// is ImageUploads, shared with covers and chapter pages.
/// </summary>
public static class PostImages
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private const string Folder = "posts";

    /// <summary>A quick size check before uploading. Returns a message to show, or null if it's fine.</summary>
    public static string? Check(IBrowserFile file) =>
        file.Size > MaxBytes
            ? $"That image is {file.Size / (1024.0 * 1024.0):F1} MB, and the limit is {MaxBytes / (1024 * 1024)} MB."
            : null;

    /// <summary>Saves the image and returns its address. Throws InvalidImageException if it isn't a real image.</summary>
    public static Task<string> SaveAsync(IWebHostEnvironment env, IBrowserFile file) =>
        ImageUploads.SaveAsync(env, file, Folder, MaxBytes);

    public static void Delete(IWebHostEnvironment env, string? url) =>
        ImageUploads.Delete(env, Folder, url);
}