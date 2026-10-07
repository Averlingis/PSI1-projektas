namespace PSI1.Api.Models;

// database row for AnswerOption. created only from an already validated option
public class AnswerOptionRow
{
    public int Id { get; private set; }
    public string OptionText { get; private set; }
    public bool IsCorrect { get; private set; }

    private AnswerOptionRow()
    {
        OptionText = string.Empty;
    }

    public AnswerOptionRow(string optionText, bool isCorrect)
    {
        OptionText = optionText;
        IsCorrect = isCorrect;
    }

}