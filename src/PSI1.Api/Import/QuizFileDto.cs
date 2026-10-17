using PSI1.Api.Models;

namespace PSI1.Api.Import;

public record QuizFileDto(
    string? Title,
    string? Description,
    string? Category,
    string? Language,
    IReadOnlyList<QuestionFileDto>? Questions
);