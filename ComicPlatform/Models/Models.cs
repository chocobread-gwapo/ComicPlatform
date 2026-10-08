using System.ComponentModel.DataAnnotations;
using ComicPlatform.Data;

namespace ComicPlatform.Models;

// If ApplicationUser lives in a different namespace in your project,
// add a "using" for it here instead of relying on this file's namespace.

public enum SeriesStatus
{
    Ongoing,
    Completed,
    Hiatus
}

/// <summary>
/// A comic/webtoon title, e.g. "The Last Signal" — the top-level thing a reader follows.
/// </summary>
public class Series
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Synopsis { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public SeriesStatus Status { get; set; } = SeriesStatus.Ongoing;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // The creator who owns this series
    public string CreatorId { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;

    // Optional: one of the genres below, designated as the primary one. Separate
    // relationship from the Genres collection, not a member of it automatically —
    // picking a Main Genre doesn't add it to Genres, and it must be configured
    // explicitly in ApplicationDbContext since EF can't infer which of the two
    // Series-to-Genre relationships this is.
    public int? MainGenreId { get; set; }
    public Genre? MainGenre { get; set; }

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<SeriesAuthor> Authors { get; set; } = new List<SeriesAuthor>();
}

/// <summary>
/// A tag like "Fantasy" or "Romance" — many-to-many with Series.
/// </summary>
public class Genre
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Series> Series { get; set; } = new List<Series>();
}

/// <summary>
/// A free-typed keyword a creator attaches to their Series (distinct from Genre,
/// which is a fixed, curated list). Many-to-many with Series, same implicit-join
/// convention as Genre — no explicit join entity needed.
/// Name is stored and matched case-insensitively at save time so "Fantasy" and
/// "fantasy" reuse the same row instead of creating near-duplicates.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Series> Series { get; set; } = new List<Series>();
}

/// <summary>
/// A single episode within a Series — what a reader actually opens to read.
/// </summary>
public class Chapter
{
    public int Id { get; set; }

    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;

    public bool IsDraft { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Page> Pages { get; set; } = new List<Page>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public int ViewCount { get; set; }
}

/// <summary>
/// One image within a Chapter, in reading order.
/// </summary>
public class Page
{
    public int Id { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public int PageNumber { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// A reader comment on a Chapter, with optional threaded replies.
/// </summary>
public class Comment
{
    public int Id { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}

/// <summary>
/// Join entity: which readers follow which series. Composite key (UserId, SeriesId).
/// </summary>
public class Follow
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastViewedAt { get; set; }
}

/// <summary>
/// Join entity: which readers follow which creator (not the same as following a Series).
/// Composite key (UserId, CreatorId). Both sides point at ApplicationUser, so each
/// relationship is configured separately in ApplicationDbContext — see the Follow
/// cascade-path note there.
/// </summary>
public class CreatorFollow
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string CreatorId { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;

    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An update a creator posts to their own profile feed — separate from a Chapter,
/// not tied to any one Series.
/// </summary>
public class CreatorPost
{
    public int Id { get; set; }

    public string CreatorId { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CreatorPostComment> Comments { get; set; } = new List<CreatorPostComment>();
    public ICollection<CreatorPostLike> Likes { get; set; } = new List<CreatorPostLike>();
}

/// <summary>
/// A reader reply on a CreatorPost. Same single-level threading as Comment
/// (a reply can't itself be replied to) — kept as its own entity rather than
/// extending Comment, since Comment is Chapter-specific elsewhere in the app
/// and reporting isn't wired up for this yet, same as chapters aren't reportable.
/// </summary>
public class CreatorPostComment
{
    public int Id { get; set; }

    public int CreatorPostId { get; set; }
    public CreatorPost CreatorPost { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ParentCommentId { get; set; }
    public CreatorPostComment? ParentComment { get; set; }
    public ICollection<CreatorPostComment> Replies { get; set; } = new List<CreatorPostComment>();
}

/// <summary>
/// Join entity: which readers liked which creator post. Composite key (UserId, CreatorPostId).
/// Simple on/off like, same as the existing Chapter Like — not a multi-reaction picker.
/// </summary>
public class CreatorPostLike
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CreatorPostId { get; set; }
    public CreatorPost CreatorPost { get; set; } = null!;

    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Join entity: which readers liked which chapter. Composite key (UserId, ChapterId).
/// </summary>
public class Like
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Join entity: which users can co-manage a series alongside its original creator.
/// </summary>
public class SeriesAuthor
{
    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public enum ReportReason { Spam, HarassmentOrHate, Spoilers, Copyright, Inappropriate, Other }
public enum ReportStatus { Pending, Resolved, Dismissed }
public enum ReportTargetType { Comment, Series }

public class Report
{
    public int Id { get; set; }
    public ReportTargetType TargetType { get; set; }
    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }
    public int? SeriesId { get; set; }
    public Series? Series { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;
    public ReportReason Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
}