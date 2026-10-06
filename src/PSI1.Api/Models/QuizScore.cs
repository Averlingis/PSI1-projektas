namespace PSI1.Api.Models;

public class QuizScore
{
    private readonly Dictionary<int, bool> answers = new();

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int QuizId { get; private set; }

    public IReadOnlyDictionary<int, bool> Answers => answers.AsReadOnly();

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

        if (!answers.TryAdd(questionIndex, isCorrect))
        {
            throw new InvalidOperationException($"Question {questionIndex} has already been answered.");
        }
    }
}