using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace ComicPlatform.Data;

// The image addresses a series uses: its cover, plus every page image in all of its chapters.
public sealed record SeriesFiles(string? CoverUrl, List<string> PageUrls);

/// <summary>
/// Deleting a series removes its rows from the database (chapters and pages go with it automatically),
/// but the image FILES sit in wwwroot/uploads and would stay there forever. This is how an admin deleting a
/// series also deletes its images. Use it in two steps, in this order:
///
///   1. var files = await SeriesCleanup.CollectAsync(db, seriesId);   // BEFORE deleting: once the rows are gone,
///                                                                    // there's no way to know which files were theirs
///   2. ...delete the series and SaveChanges...
///   3. SeriesCleanup.Delete(env, files);                              // AFTER the database delete worked, so a
///                                                                    // failed delete never leaves a series with missing images
/// </summary>
public static class SeriesCleanup
{
    /// <summary>Looks up the cover and all page image addresses for a series.</summary>
    public static async Task<SeriesFiles> CollectAsync(ApplicationDbContext db, int seriesId)
    {
        // The series' own cover image (null when it never had one).
        var cover = await db.Series
            .Where(s => s.Id == seriesId)
            .Select(s => s.CoverImageUrl)
            .FirstOrDefaultAsync();

        // Every page image from every chapter of this series.
        var pages = await db.Pages
            .Where(p => p.Chapter.SeriesId == seriesId)
            .Select(p => p.ImageUrl)
            .ToListAsync();

        return new SeriesFiles(cover, pages);
    }

    /// <summary>Deletes those image files from disk.</summary>
    public static void Delete(IWebHostEnvironment env, SeriesFiles files)
    {
        // ImageUploads.Delete only removes files inside the folder it's given (covers or pages), and ignores
        // any address that doesn't belong there, so a wrong or old-style address can't delete anything else.
        ImageUploads.Delete(env, "covers", files.CoverUrl);

        foreach (var url in files.PageUrls)
        {
            ImageUploads.Delete(env, "pages", url);
        }
    }
}
