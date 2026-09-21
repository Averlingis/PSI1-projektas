using System.ComponentModel.DataAnnotations;

namespace PSI1.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(3), MaxLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and hyphens.")]
    string Username,
    [Required, MinLength(8), MaxLength(100)] string Password
);
