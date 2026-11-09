using PSI1.Api.Models;

namespace PSI1.Api.QuizBuilding;

public record QuizBuildResult(
    Quiz? Quiz,
    IReadOnlyList<QuizError> Errors
)
{
    public bool IsSuccess => Quiz is not null;
}