namespace PSI1.Api.Models;

public class Question
{
    public const int MinOptions = 2;
    public const int MaxOptions = 8;
    public const int MaxQuestionTextLength = 200;

    public string QuestionText { get; set; }
    public List<AnswerOption> Options { get; set; }

    public Question(string questionText, List<AnswerOption> options)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            throw new ArgumentException("Question text is required.", nameof(questionText));
        }

        if (questionText.Length > MaxQuestionTextLength)
        {
            throw new ArgumentException($"Max question text length is {MaxQuestionTextLength} symbols.", nameof(questionText));
        }

        ArgumentNullException.ThrowIfNull(options);

        if (options.Count < MinOptions || options.Count > MaxOptions)
        {
            throw new ArgumentException(
                $"A question must have between {MinOptions} and {MaxOptions} options.",
                nameof(options));
        }

        if (options.Count(option => option.IsCorrect) != 1)
        {
            throw new ArgumentException(
                "Exactly one option must be marked as correct.", nameof(options));
        }

        QuestionText = questionText;
        Options = [.. options]; // copy
    }
}