using System.Globalization;
using System.Security.Claims;
using ComicPlatform.Models;

namespace ComicPlatform.Data;

/// <summary>
/// One place for the age rules. The viewer's date of birth is read from the "BirthDate"
/// claim that CustomUserClaimsPrincipalFactory puts in the sign-in cookie, and the age
/// is computed from it each time, so someone who turns 16 starts seeing 16+ series
/// with no re-login.
///
/// Because AgeRating is declared in ascending order, the visibility check for a list is:
///   var max = AgeGate.MaxRatingFor(user);
///   query.Where(s => s.Rating <= max)
/// </summary>
public static class AgeGate
{
    /// <summary>The youngest age that can have an account. Change it here if the policy changes.</summary>
    public const int MinimumSignupAge = 13;

    public static int MinimumAge(this AgeRating rating) => rating switch
    {
        AgeRating.Age18 => 18,
        AgeRating.Age16 => 16,
        _ => 13
    };

    public static string Label(this AgeRating rating) => $"{rating.MinimumAge()}+";

    public static int AgeOn(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }
        return age;
    }

    /// <summary>The highest rating someone of this age may see. Unknown age counts as the lowest tier.</summary>
    public static AgeRating MaxRatingForAge(int? age) => age switch
    {
        >= 18 => AgeRating.Age18,
        >= 16 => AgeRating.Age16,
        _ => AgeRating.Age13
    };

    public static int? AgeOf(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("BirthDate");
        if (value is not null
            && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
        {
            return AgeOn(birthDate, DateOnly.FromDateTime(DateTime.UtcNow));
        }

        return null;
    }

    /// <summary>
    /// The highest rating this viewer may see. Logged-out visitors and anyone without a
    /// birthday get 13+ only; admins see everything.
    /// </summary>
    public static AgeRating MaxRatingFor(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin"))
        {
            return AgeRating.Age18;
        }

        return MaxRatingForAge(AgeOf(user));
    }
}
