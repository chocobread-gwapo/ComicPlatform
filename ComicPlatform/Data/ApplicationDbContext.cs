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
    public DbSet<SignupAttempt> SignupAttempts { get; set; }
    public DbSet<ChapterDailyView> ChapterDailyViews { get; set; }
    public DbSet<ChapterViewMark> ChapterViewMarks { get; set; }
    public DbSet<RevenuePeriod> RevenuePeriods { get; set; }
    public DbSet<RevenueLine> RevenueLines { get; set; }

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

        // Looked up when counting how many codes were sent to one address.
        builder.Entity<SignupAttempt>()
            .HasIndex(a => a.Email);

        // Revenue ledger. Daily view counts are summed over a month when it's closed.
        builder.Entity<ChapterDailyView>()
            .HasKey(d => new { d.ChapterId, d.Day });

        builder.Entity<ChapterDailyView>()
            .HasIndex(d => d.Day);

        // No relationship to Chapter on purpose: these rows are short-lived and cleaned up by date.
        builder.Entity<ChapterViewMark>()
            .HasKey(m => new { m.ChapterId, m.Day, m.VisitorKey });

        builder.Entity<RevenuePeriod>().Property(p => p.TotalRevenue).HasPrecision(18, 2);
        builder.Entity<RevenuePeriod>().Property(p => p.CreatorPool).HasPrecision(18, 2);
        builder.Entity<RevenuePeriod>().Property(p => p.CreatorSharePercent).HasPrecision(5, 2);
        builder.Entity<RevenueLine>().Property(l => l.Amount).HasPrecision(18, 2);

        // A month can only be closed once, and a creator has one line per month.
        builder.Entity<RevenuePeriod>()
            .HasIndex(p => new { p.Year, p.Month })
            .IsUnique();

        builder.Entity<RevenueLine>()
            .HasIndex(l => new { l.RevenuePeriodId, l.UserId })
            .IsUnique();

        builder.Entity<RevenueLine>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Reports on creator posts, post replies and creator profiles. All Restrict: a
        // post delete already cascades to its replies, so SetNull here would give SQL
        // Server two cascade paths into Report (and ReportedCreator points at
        // AspNetUsers, same problem as CreatorFollow). The code that deletes posts and
        // replies detaches their reports first — see ReportCleanup.
        builder.Entity<Report>()
            .HasOne(r => r.CreatorPost)
            .WithMany()
            .HasForeignKey(r => r.CreatorPostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Report>()
            .HasOne(r => r.CreatorPostComment)
            .WithMany()
            .HasForeignKey(r => r.CreatorPostCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Report>()
            .HasOne(r => r.ReportedCreator)
            .WithMany()
            .HasForeignKey(r => r.ReportedCreatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}