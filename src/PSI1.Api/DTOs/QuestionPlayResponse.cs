namespace PSI1.Api.DTOs;

public record QuestionPlayResponse(
    int Position,
    string QuestionText,
    List<string> Options
);