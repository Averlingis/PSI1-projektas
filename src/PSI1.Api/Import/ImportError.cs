namespace PSI1.Api.Import;

public record ImportError(
    int? Location,
    string Message
);