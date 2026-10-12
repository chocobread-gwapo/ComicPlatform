using System.ComponentModel.DataAnnotations;

namespace ComicPlatform.Components.Account;

/// <summary>
/// Same format check as the built-in [Url] attribute, but treats an empty or
/// whitespace-only value as valid instead of rejecting it. [Url] only skips
/// validation for an actual null — a static form post always submits *something*
/// for every field, so an untouched optional field arrives as "" rather than
/// null, and [Url] fails it even though the person never typed anything in.
/// </summary>
public class OptionalUrlAttribute : ValidationAttribute
{
    public OptionalUrlAttribute()
    {
        ErrorMessage = "The {0} field is not a valid fully-qualified http or https URL.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
