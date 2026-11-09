using PSI1.Api.Models;

namespace PSI1.Api.DTOs;

public record QuizDto(
    string? Title,
    string? Description,
    string? Category,
    string? Language,
    IReadOnlyList<QuestionDto?>? Questions
);