using PSI1.Api.Models;

namespace PSI1.Api.Import;

public record QuizImportResult(
    Quiz? Quiz,
    IReadOnlyList<ImportError> Errors
)
{
    public bool IsSuccess => Quiz is not null;
}