using Microsoft.EntityFrameworkCore;

namespace ComicPlatform.Data;

public sealed record CreatorViews(string UserId, string DisplayName, long Views);

public sealed record RevenueLinePreview(string UserId, string DisplayName, long Views, decimal Amount);

public sealed record RevenuePreview(
    int Year,
    int Month,
    decimal TotalRevenue,
    decimal SharePercent,
    decimal Pool,
    long TotalViews,
    decimal Allocated,
    List<RevenueLinePreview> Lines);

/// <summary>
/// The rules for dividing a month's ad revenue. Both the admin preview and the real close use
/// this, so what you preview is exactly what gets saved.
///
///   1. Views come from the daily counts for that month (UTC days).
///   2. A series' views are split equally between its creator and its co-authors. Any odd view
///      left over goes to the creator, so the numbers always add back up.
///   3. The creators' pool is the total revenue times the creators' percentage.
///   4. Each person gets pool * (their views / everyone's views), rounded DOWN to the cent.
///      The few cents left over by rounding stay with the site.
/// </summary>
public static class RevenueCalculator
{
    public static async Task<List<CreatorViews>> ViewsByCreatorAsync(ApplicationDbContext db, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var perSeries = await db.ChapterDailyViews
            .Where(d => d.Day >= start && d.Day < end)
            .GroupBy(d => d.Chapter.SeriesId)
            .Select(g => new { SeriesId = g.Key, Views = g.Sum(d => (long)d.Views) })
            .ToListAsync();

        if (perSeries.Count == 0)
        {
            return new List<CreatorViews>();
        }

        var seriesIds = perSeries.Select(p => p.SeriesId).ToList();

        var creators = await db.Series
            .Where(s => seriesIds.Contains(s.Id))
            .Select(s => new { s.Id, s.CreatorId })
            .ToDictionaryAsync(s => s.Id, s => s.CreatorId);

        var coAuthors = await db.SeriesAuthors
            .Where(a => seriesIds.Contains(a.SeriesId))
            .Select(a => new { a.SeriesId, a.UserId })
            .ToListAsync();

        var totals = new Dictionary<string, long>();

        foreach (var series in perSeries)
        {
            if (!creators.TryGetValue(series.SeriesId, out var creatorId))
            {
                continue;
            }

            var others = coAuthors
                .Where(a => a.SeriesId == series.SeriesId && a.UserId != creatorId)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            var people = others.Count + 1;
            var each = series.Views / people;
            var leftover = series.Views - each * people;

            totals[creatorId] = totals.GetValueOrDefault(creatorId) + each + leftover;

            foreach (var userId in others)
            {
                totals[userId] = totals.GetValueOrDefault(userId) + each;
            }
        }

        var ids = totals.Keys.ToList();
        var names = await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        return totals
            .Select(t => new CreatorViews(t.Key, names.GetValueOrDefault(t.Key) ?? "(unknown)", t.Value))
            .OrderByDescending(v => v.Views)
            .ThenBy(v => v.DisplayName)
            .ToList();
    }

    public static RevenuePreview Build(int year, int month, decimal totalRevenue, decimal sharePercent, List<CreatorViews> views)
    {
        var pool = RoundDown(totalRevenue * sharePercent / 100m);
        var totalViews = views.Sum(v => v.Views);

        var lines = new List<RevenueLinePreview>();

        if (totalViews > 0)
        {
            foreach (var v in views)
            {
                lines.Add(new RevenueLinePreview(v.UserId, v.DisplayName, v.Views, RoundDown(pool * v.Views / totalViews)));
            }
        }

        return new RevenuePreview(year, month, totalRevenue, sharePercent, pool, totalViews, lines.Sum(l => l.Amount), lines);
    }

    private static decimal RoundDown(decimal value) => Math.Round(value, 2, MidpointRounding.ToZero);
}
