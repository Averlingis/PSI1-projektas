using PSI1.Api.DTOs;
using PSI1.Api.Models;
using PSI1.Api.QuizBuilding;

namespace PSI1.Api.Tests.QuizBuilding;

public class QuizBuilderTests
{
    private static QuizDto ValidDto() => new(
        Title: "Family Vocabulary",
        Description: null,
        Category: "Family",
        Language: "Lithuanian",
        Questions:
        [
            new QuestionDto("Question 1?", [new OptionDto("Right", true), new OptionDto("Wrong", false)]),
            new QuestionDto("Question 2?", [new OptionDto("Right", true), new OptionDto("Wrong", false)]),
            new QuestionDto("Question 3?", [new OptionDto("Right", true), new OptionDto("Wrong", false)])
        ]);

    [Fact]
    public void Build_ValidDto_ReturnsQuiz()
    {
        var dto = ValidDto();

        var result = new QuizBuilder().Build(dto);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Quiz);
        Assert.Equal("Family Vocabulary", result.Quiz.Title);
        Assert.Equal(Category.Family, result.Quiz.Category);
        Assert.Equal(Language.Lithuanian, result.Quiz.Language);
        Assert.Equal(3, result.Quiz.Questions.Count);
    }

    [Fact]
    public void Build_UnknownCategory_ReturnsError()
    {
        var dto = ValidDto() with { Category = "Movies" };

        var result = new QuizBuilder().Build(dto);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("Movies", error.Message);
    }

    [Fact]
    public void Build_UnknownLanguage_ReturnsError()
    {
        var dto = ValidDto() with { Language = "Polish" };

        var result = new QuizBuilder().Build(dto);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("Polish", error.Message);
    }

    [Fact]
    public void Build_InvalidQuestion_ReturnsErrorWithQuestionNumber()
    {
        var valid = ValidDto();
        var questions = valid.Questions!.ToList();
        questions[1] = questions[1]! with { Text = "" };
        var dto = valid with { Questions = questions };

        var result = new QuizBuilder().Build(dto);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(2, error.Location);
    }
}