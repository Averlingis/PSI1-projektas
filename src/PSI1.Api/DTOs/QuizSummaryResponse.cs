using PSI1.Api.Models;

namespace PSI1.Api.DTOs;

public record QuizSummaryResponse(
    int Id,
    string Title,
    string? Description,
    Category Category,
    Language Language,
    int QuestionCount);