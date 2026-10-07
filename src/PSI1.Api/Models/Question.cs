namespace PSI1.Api.Models;

public class Question
{
    public const int MinOptions = 2;
    public const int MaxOptions = 8;
    public const int MaxQuestionTextLength = 200;

    private readonly List<AnswerOptionRow> optionRows = new();

    public int Id { get; private set; }
    public string QuestionText { get; private set; }
    public IReadOnlyList<AnswerOption> Options => optionRows.Select(row => new AnswerOption(row.OptionText, row.IsCorrect)).ToList();

    private Question()
    {
        QuestionText = string.Empty;
    }
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
        optionRows.AddRange(options.Select(option => new AnswerOptionRow(option.OptionText, option.IsCorrect)));
    }
}