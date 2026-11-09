using PSI1.Api.Models;

namespace PSI1.Api.Tests.Models;

public class QuizScoreTests
{
    private static QuizScore CreateScore(params int[] answeredIndexes)
    {
        var score = new QuizScore(userId: 1, quizId: 1);

        foreach (var index in answeredIndexes)
        {
            score.RecordAnswer(index, isCorrect: true);
        }

        return score;
    }

    [Theory]
    [InlineData(new int[] { }, 5, 0)]            //nothing answered
    [InlineData(new[] { 0 }, 5, 1)]
    [InlineData(new[] { 0, 1, 2 }, 5, 3)]
    [InlineData(new[] { 2, 0 }, 5, 1)]           //answered out of order, question 1 is still open
    [InlineData(new[] { 4, 3, 2, 1 }, 5, 0)]     //only the first question is missing
    [InlineData(new[] { 0, 1, 3, 4 }, 5, 2)]     //gap in the middle
    [InlineData(new[] { 7, 9 }, 5, 0)]           //answers outside the question count are ignored
    public void GetFirstUnansweredIndex_SomeQuestionsOpen_ReturnsLowestOpenIndex(
        int[] answeredIndexes, int questionCount, int expected)
    {
        var score = CreateScore(answeredIndexes);

        Assert.Equal(expected, score.GetFirstUnansweredIndex(questionCount));
    }

    [Theory]
    [InlineData(new[] { 0, 1, 2 }, 3)]
    [InlineData(new[] { 2, 0, 1 }, 3)]           //all answered, in any order
    [InlineData(new[] { 0, 1, 2, 3, 4 }, 3)]     //more answers than questions
    [InlineData(new int[] { }, 0)]               //a quiz without questions has nothing left to answer
    public void GetFirstUnansweredIndex_AllQuestionsAnswered_ReturnsNull(int[] answeredIndexes, int questionCount)
    {
        var score = CreateScore(answeredIndexes);

        Assert.Null(score.GetFirstUnansweredIndex(questionCount));
    }

    [Fact]
    public void GetFirstUnansweredIndex_NegativeQuestionCount_Throws()
    {
        var score = CreateScore();

        Assert.Throws<ArgumentOutOfRangeException>(() => score.GetFirstUnansweredIndex(-1));
    }

    [Fact]
    public void GetFirstUnansweredIndex_AfterAnsweringIt_MovesToNextOpenIndex()
    {
        var score = CreateScore(0);
        Assert.Equal(1, score.GetFirstUnansweredIndex(3));

        score.RecordAnswer(1, isCorrect: false);

        Assert.Equal(2, score.GetFirstUnansweredIndex(3));
    }

    [Fact]
    public void GetLatestAnswerId_NoAnswers_ReturnsNull()
    {
        var score = CreateScore();

        Assert.Null(score.GetLatestAnswerId());
    }
}