namespace ComicPlatform.Components.Shared;

// View models for the creator feed — not EF entities, just the shape the
// CreatorProfile page and CreatorPostCard component pass between them.

public class CreatorPostItem
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public List<CreatorPostReplyItem> Replies { get; set; } = new();
}

public class CreatorPostReplyItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
