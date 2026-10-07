namespace PSI1.Api.Models;

public class Quiz
{
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MinQuestions = 3;
    public const int MaxQuestions = 100;

    private readonly List<Question> questions = new();

    public int Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Category Category { get; private set; }
    public Language Language { get; private set; }
    public IReadOnlyList<Question> Questions => questions.AsReadOnly();

    private Quiz()
    {
        Title = string.Empty;
    }
    public Quiz(string title, Category category, Language language, List<Question> questions, string? description = null)
    {
        // Title validation
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Quiz title is required.", nameof(title));
        }
        if (title.Length > MaxTitleLength)
        {
            throw new ArgumentException($"Max title length is {MaxTitleLength} symbols.", nameof(title));
        }

        // Description validation
        if (description is not null && description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Max description length is {MaxDescriptionLength} symbols.", nameof(description));
        }

        // Questions validation
        ArgumentNullException.ThrowIfNull(questions);

        if (questions.Count < MinQuestions || questions.Count > MaxQuestions)
        {
            throw new ArgumentException(
                $"A quiz must have between {MinQuestions} and {MaxQuestions} questions.", nameof(questions));
        }

        Title = title;
        Description = description;
        Category = category;
        Language = language;
        this.questions = [.. questions];
    }
}