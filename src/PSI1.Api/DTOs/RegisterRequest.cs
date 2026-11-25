using System.ComponentModel.DataAnnotations;

namespace PSI1.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(UsernameRules.MinLength), MaxLength(UsernameRules.MaxLength)]
    [RegularExpression(UsernameRules.AllowedCharacters, ErrorMessage = UsernameRules.ErrorMessage)]
    string Username,
    [Required, MinLength(8), MaxLength(100)] string Password
);