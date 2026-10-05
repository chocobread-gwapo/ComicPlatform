using Microsoft.AspNetCore.Identity;

namespace ComicPlatform.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? StripeConnectAccountId { get; set; }
    public bool StripeOnboardingComplete { get; set; }
}