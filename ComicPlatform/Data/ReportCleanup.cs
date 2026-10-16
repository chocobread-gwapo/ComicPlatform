using Microsoft.EntityFrameworkCore;
using ComicPlatform.Models;

namespace ComicPlatform.Data;

/// <summary>
/// Report's links to CreatorPost / CreatorPostComment are Restrict (SetNull would give
/// SQL Server multiple cascade paths), so reports have to be detached by hand before
/// the content they point at is deleted. Each method detaches the links, closes any
/// pending reports (there's nothing left to act on) and saves — the caller then
/// deletes the content in a separate SaveChanges, so the order is guaranteed.
/// The report rows stay, so the written reason is still in the Resolved list.
/// </summary>
public static class ReportCleanup
{
    /// <summary>For deleting one creator post: covers reports on the post and on all its replies.</summary>
    public static async Task DetachForCreatorPostAsync(ApplicationDbContext db, int postId)
    {
        var replyIds = await db.CreatorPostComments
            .Where(c => c.CreatorPostId == postId)
            .Select(c => c.Id)
            .ToListAsync();

        var affected = await db.Reports
            .Where(r => r.CreatorPostId == postId
                || (r.CreatorPostCommentId != null && replyIds.Contains(r.CreatorPostCommentId.Value)))
            .ToListAsync();

        foreach (var r in affected)
        {
            r.CreatorPostId = null;
            r.CreatorPostCommentId = null;
            CloseIfPending(r);
        }

        if (affected.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    /// <summary>For deleting creator post replies: pass the reply and every reply beneath it.</summary>
    public static async Task DetachForCreatorPostCommentsAsync(ApplicationDbContext db, List<int> commentIds)
    {
        if (commentIds.Count == 0)
        {
            return;
        }

        var affected = await db.Reports
            .Where(r => r.CreatorPostCommentId != null && commentIds.Contains(r.CreatorPostCommentId.Value))
            .ToListAsync();

        foreach (var r in affected)
        {
            r.CreatorPostCommentId = null;
            CloseIfPending(r);
        }

        if (affected.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private static void CloseIfPending(Report r)
    {
        if (r.Status == ReportStatus.Pending)
        {
            r.Status = ReportStatus.Resolved;
        }
    }
}
