namespace PSI1.Api.DTOs;

public record CreateQuizRequest(
    QuizDto Quiz,
    bool IsPublic
); 