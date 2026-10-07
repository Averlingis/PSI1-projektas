namespace PSI1.Api.Models;

public class QuestionAnswer
{
    public int Id { get; private set; }
    public int QuizScoreId { get; private set; }
    public int QuestionIndex { get; private set; }
    public bool IsCorrect { get; private set; }

    private QuestionAnswer()
    {
    }

    public QuestionAnswer(int questionIndex, bool isCorrect)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(questionIndex);

        QuestionIndex = questionIndex;
        IsCorrect = isCorrect;
    }
}