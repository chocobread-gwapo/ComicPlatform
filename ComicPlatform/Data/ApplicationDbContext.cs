using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ComicPlatform.Models;

namespace ComicPlatform.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Series> Series { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<CreatorFollow> CreatorFollows { get; set; }
    public DbSet<CreatorPost> CreatorPosts { get; set; }
    public DbSet<CreatorPostComment> CreatorPostComments { get; set; }
    public DbSet<CreatorPostLike> CreatorPostLikes { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<SeriesAuthor> SeriesAuthors { get; set; }
    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Series>()
            .HasOne(s => s.Creator)
            .WithMany()
            .HasForeignKey(s => s.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<Series>()
            .HasOne(s => s.MainGenre)
            .WithMany()
            .HasForeignKey(s => s.MainGenreId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Follow>()
            .HasKey(f => new { f.UserId, f.SeriesId });

        builder.Entity<CreatorFollow>()
            .HasKey(cf => new { cf.UserId, cf.CreatorId });

        // Both FKs point at ApplicationUser (AspNetUsers), so leaving either one on
        // Cascade would give SQL Server two cascade paths into the same table from a
        // single user delete — the same "multiple cascade paths" migration failure
        // already hit with Report. Both sides are Restrict here instead, same as
        // Comment's self-referencing ParentComment relationship above.
        builder.Entity<CreatorFollow>()
            .HasOne(cf => cf.User)
            .WithMany()
            .HasForeignKey(cf => cf.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CreatorFollow>()
            .HasOne(cf => cf.Creator)
            .WithMany()
            .HasForeignKey(cf => cf.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CreatorPost>()
            .HasOne(cp => cp.Creator)
            .WithMany()
            .HasForeignKey(cp => cp.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Same reasoning as CreatorFollow above: direct FKs to ApplicationUser stay
        // Restrict. CreatorPostComment.CreatorPostId and CreatorPostLike.CreatorPostId
        // are left on EF's default cascade — deleting a post should take its own
        // comments and likes with it, same as Chapter -> Comment isn't overridden either.
        builder.Entity<CreatorPostComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CreatorPostComment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CreatorPostLike>()
            .HasKey(l => new { l.UserId, l.CreatorPostId });

        builder.Entity<CreatorPostLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Like>()
            .HasKey(l => new { l.UserId, l.ChapterId });

        builder.Entity<SeriesAuthor>()
            .HasKey(sa => new { sa.SeriesId, sa.UserId });

        builder.Entity<Report>()
            .HasOne(r => r.Series)
            .WithMany()
            .HasForeignKey(r => r.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);   // was SetNull

        builder.Entity<Report>()
            .HasOne(r => r.Comment)
            .WithMany()
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Restrict);
    }
}