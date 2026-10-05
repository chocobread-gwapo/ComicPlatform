using System.Security.Claims;
using ComicPlatform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ComicPlatform.Components.Account;

// Identity's cookie claims cover UserName/Email/etc. by default, but not our
// custom DisplayName — this adds it as its own claim at sign-in so pages can
// read it straight off ClaimsPrincipal (e.g. in NavMenu) without a DB lookup
// on every render.
//
// Inherits from the two-type-parameter UserClaimsPrincipalFactory<ApplicationUser,
// IdentityRole> rather than the one-parameter version. The one-parameter base never
// adds role claims to the sign-in cookie, no matter what's assigned in the database —
// that's why AuthorizeView Roles="Admin" wasn't matching even for a user the database
// correctly had as Admin. The two-parameter base handles that automatically.
public class CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            identity.AddClaim(new Claim("DisplayName", user.DisplayName));
        }

        return principal;
    }
}