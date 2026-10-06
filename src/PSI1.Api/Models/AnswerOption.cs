namespace PSI1.Api.Models;

public readonly struct AnswerOption
{
    public const int MaxOptionTextLength = 100;

    public string OptionText { get; }
    public bool IsCorrect { get; }

    public AnswerOption(string optionText, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(optionText))
        {
            throw new ArgumentException("Option text is required.", nameof(optionText));
        }

        if (optionText.Length > MaxOptionTextLength)
        {
            throw new ArgumentException($"Max option text length is {MaxOptionTextLength} symbols.", nameof(optionText));
        }

        OptionText = optionText;
        IsCorrect = isCorrect;
    }
}