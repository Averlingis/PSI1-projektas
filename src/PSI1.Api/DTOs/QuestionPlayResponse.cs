namespace PSI1.Api.DTOs;

public record QuestionPlayResponse(
    int QuestionId,
    int Position,
    string QuestionText,
    List<string> Options
);