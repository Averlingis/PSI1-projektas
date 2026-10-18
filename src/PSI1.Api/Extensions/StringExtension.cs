namespace PSI1.Api.Extensions;

public static class StringExtensions
{
    public static string ToLowerTrimmed(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        // invariant better because we work with different languages
        return value.Trim().ToLowerInvariant();
    }
}