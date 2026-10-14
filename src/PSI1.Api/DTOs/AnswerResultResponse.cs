namespace PSI1.Api.DTOs;

public record AnswerResultResponse(
    bool IsCorrect,
    string CorrectOptionText
);