namespace PSI1.Api.DTOs;

public record QuizProgressResponse(
    int? NextQuestionIndex,
    int AnsweredCount,
    int TotalQuestions
);