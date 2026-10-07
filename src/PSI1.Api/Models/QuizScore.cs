namespace PSI1.Api.Models;

public class QuizScore
{
    private readonly List<QuestionAnswer> answerRows = new();

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int QuizId { get; private set; }

    public IReadOnlyDictionary<int, bool> Answers => answerRows.ToDictionary(a => a.QuestionIndex, a => a.IsCorrect);

    private QuizScore()
    {
    }

    public QuizScore(int userId, int quizId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quizId);

        UserId = userId;
        QuizId = quizId;
    }

    public void RecordAnswer(int questionIndex, bool isCorrect)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(questionIndex);

        if (answerRows.Any(a => a.QuestionIndex == questionIndex))
        {
            throw new InvalidOperationException($"Question {questionIndex} has already been answered.");
        }

        answerRows.Add(new QuestionAnswer(questionIndex, isCorrect));
    }
}