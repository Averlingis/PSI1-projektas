namespace PSI1.Api.DTOs;

public record LoginRequest(
    string Email,
    string Password
);