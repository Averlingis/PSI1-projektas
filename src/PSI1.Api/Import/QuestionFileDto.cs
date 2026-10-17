namespace PSI1.Api.Import;

public record QuestionFileDto(
    string? Text,
    IReadOnlyList<OptionFileDto>? Options
);