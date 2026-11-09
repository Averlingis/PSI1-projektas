namespace PSI1.Api.DTOs;

public record QuestionDto(
    string? Text,
    IReadOnlyList<OptionDto?>? Options
);