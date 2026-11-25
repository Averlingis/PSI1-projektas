namespace PSI1.Api.DTOs;

public static class UsernameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 30;
    public const string AllowedCharacters = @"^[a-zA-Z0-9_-]+$";
    public const string ErrorMessage =
        "Username can only contain letters, numbers, underscores, and hyphens.";
}