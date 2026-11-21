namespace PSI1.Api.Helpers;

// Shared helpers for enums like Category and Language,
// so the same enum code is not written separately for each of them.
public static class EnumValues<TEnum> where TEnum : struct, Enum
{
    // Returns the names of all values as strings.
    public static List<string> Names() =>
        Enum.GetValues<TEnum>()
            .Select(value => value.ToString())
            .ToList();

    // Checks that a value is one of the enum's members.
    public static bool IsDefined(TEnum value) =>
        Enum.IsDefined(value);

    // Parses a name ignoring case. Returns false for unknown names and undefined numbers.
    public static bool TryParse(string? text, out TEnum value) =>
        Enum.TryParse(text, ignoreCase: true, out value) && IsDefined(value);
}