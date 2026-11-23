using PSI1.Api.Models;

namespace PSI1.Api.DTOs;

public record CategoryStatsResponse(
    Category Category,
    int Answered,
    int Correct,
    double? Accuracy,
    bool HasEnoughData
);