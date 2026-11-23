using PSI1.Api.Models;

namespace PSI1.Api.Services;

public record CategoryAnswerCounts(
    Category Category,
    int Answered,
    int Correct
);