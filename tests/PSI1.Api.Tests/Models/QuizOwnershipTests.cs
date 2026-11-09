using PSI1.Api.Models;

namespace PSI1.Api.Tests.Models;

public class QuizOwnershipTests
{
    private static List<Question> CreateQuestions() =>
        Enumerable.Range(1, Quiz.MinQuestions)
            .Select(i => new Question($"Question {i}",
            [
                new AnswerOption("Right", true),
                 new AnswerOption("Wrong", false)
            ]))
            .ToList();

    private static Quiz CreateQuiz(int? ownerId = null, bool isPublic = true) =>
        new("Food Basics", Category.Food, Language.Lithuanian, CreateQuestions(), ownerId: ownerId, isPublic: isPublic);

    [Fact]
    public void Constructor_Defaults_QuizIsPublicWithNoOwner()
    {
        var quiz = new Quiz("Food Basics", Category.Food, Language.Lithuanian, CreateQuestions());

        Assert.Null(quiz.OwnerId);
        Assert.True(quiz.IsPublic);
    }
    [Fact]
    public void Constructor_OwnerWithPrivateQuiz()
    {
        var quiz = CreateQuiz(ownerId: 5, isPublic: false);

        Assert.Equal(5, quiz.OwnerId);
        Assert.False(quiz.IsPublic);
    }
    [Fact]
    public void Constructor_OwnerWithPublicQuiz()
    {
        var quiz = CreateQuiz(ownerId: 5, isPublic: true);

        Assert.Equal(5, quiz.OwnerId);
        Assert.True(quiz.IsPublic);
    }

    [Fact]
    public void Constructor_PrivateQuizWithNoOwner_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateQuiz(ownerId: null, isPublic: false));

    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveOwnerId_Throws(int ownerId)
    {
        Assert.Throws<ArgumentException>(() => CreateQuiz(ownerId: ownerId));
    }

}