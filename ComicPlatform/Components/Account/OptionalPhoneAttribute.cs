using System.ComponentModel.DataAnnotations;

namespace ComicPlatform.Components.Account;

/// <summary>
/// Same gap as OptionalUrlAttribute, for phone numbers: the built-in [Phone]
/// attribute only skips validation for an actual null, and a static form post
/// always submits "" for an untouched optional field, not null. Delegates the
/// real format check to the built-in PhoneAttribute for anything actually typed
/// in, rather than reimplementing phone-format rules from scratch.
/// </summary>
public class OptionalPhoneAttribute : ValidationAttribute
{
    private static readonly PhoneAttribute InnerPhoneValidator = new();

    public OptionalPhoneAttribute()
    {
        ErrorMessage = "The {0} field is not a valid phone number.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return InnerPhoneValidator.IsValid(text);
    }
}
