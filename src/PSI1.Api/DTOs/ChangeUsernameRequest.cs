using System.ComponentModel.DataAnnotations;

namespace PSI1.Api.DTOs;

public record ChangeUsernameRequest(
    [Required]
    [MinLength(UsernameRules.MinLength)]
    [MaxLength(UsernameRules.MaxLength)]
    [RegularExpression(
        UsernameRules.AllowedCharacters,
        ErrorMessage = UsernameRules.ErrorMessage)]
    string Username
);