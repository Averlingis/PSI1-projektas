using PSI1.Api.Models;

namespace PSI1.Api.DTOs;

public record ProgressResponse(
    List<CategoryStatsResponse> Categories,
    Category? Strongest,
    Category? Weakest
);