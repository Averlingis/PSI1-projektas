namespace PSI1.Api.Models;

public readonly struct AnswerOption
{
	public string OptionText { get; }
	public bool IsCorrect { get; }

	public AnswerOption(string optionText, bool isCorrect)
	{
		OptionText = optionText;
		IsCorrect = isCorrect;
	}
}
