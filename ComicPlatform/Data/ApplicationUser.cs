using Microsoft.AspNetCore.Identity;

namespace ComicPlatform.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Self-declared date of birth. Used only to work out which age-rated series this
    // person can see (see AgeGate). Never shown on profiles.
    public DateOnly? BirthDate { get; set; }
    public string? StripeConnectAccountId { get; set; }
    public bool StripeOnboardingComplete { get; set; }

    // Optional links shown on the creator profile page
    public string? InstagramUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? PatreonUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}