namespace PSI1.Api.DTOs;

public record QuizScoreResponse(
    int CorrectAnswers,
    int TotalAnswered
);