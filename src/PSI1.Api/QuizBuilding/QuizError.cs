namespace PSI1.Api.QuizBuilding;

public record QuizError(
    int? Location,
    string Message
);