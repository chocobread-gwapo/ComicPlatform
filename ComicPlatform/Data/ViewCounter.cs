using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ComicPlatform.Models;

namespace ComicPlatform.Data;

/// <summary>
/// Records a chapter view, at most once per visitor per chapter per day (UTC).
///
/// "Already counted?" is answered by the database itself: it tries to insert a ChapterViewMark for
/// (chapter, today, visitor), and a repeat hits the primary key and is skipped. That also covers two
/// tabs opening the same chapter at the same moment. When a view does count, both counters are
/// bumped with a single UPDATE (not read, add one, save), so simultaneous readers can't overwrite each other.
/// </summary>
public static class ViewCounter
{
    private static DateOnly _lastCleanupDay;

    public static async Task RecordAsync(IDbContextFactory<ApplicationDbContext> factory, int chapterId, string visitorKey)
    {
        await using var db = await factory.CreateDbContextAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Only today's marks matter, so on the first view of each day (per app start) drop older ones.
        if (_lastCleanupDay != today)
        {
            _lastCleanupDay = today;
            await db.ChapterViewMarks.Where(m => m.Day < today).ExecuteDeleteAsync();
        }

        if (visitorKey.Length > 64)
        {
            visitorKey = visitorKey[..64];
        }

        try
        {
            db.ChapterViewMarks.Add(new ChapterViewMark { ChapterId = chapterId, Day = today, VisitorKey = visitorKey });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // Already counted this visitor for this chapter today.
            return;
        }

        db.ChangeTracker.Clear();

        // The lifetime total used by rankings and the series pages.
        await db.Chapters
            .Where(c => c.Id == chapterId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ViewCount, c => c.ViewCount + 1));

        // Today's count, which the revenue ledger adds up over a month.
        if (await AddToToday(db, chapterId, today) == 0)
        {
            try
            {
                db.ChapterDailyViews.Add(new ChapterDailyView { ChapterId = chapterId, Day = today, Views = 1 });
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Someone else created today's row a moment ago, so add to theirs instead.
                db.ChangeTracker.Clear();
                await AddToToday(db, chapterId, today);
            }
        }
    }

    private static Task<int> AddToToday(ApplicationDbContext db, int chapterId, DateOnly today) =>
        db.ChapterDailyViews
            .Where(d => d.ChapterId == chapterId && d.Day == today)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Views, d => d.Views + 1));
}