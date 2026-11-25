using PSI1.Api.Models;

namespace PSI1.Api.Tests.Models;

public class QuestionTests
{
    [Theory]
    [InlineData("Yes", "Yes")]
    [InlineData("Yes", "yes")]
    [InlineData("Yes", " Yes ")]
    [InlineData(" YES ", "yes")]
    public void Constructor_DuplicateOptions_ThrowsArgumentException(
        string first, string second)
    {
        var options = new List<AnswerOption>
        {
            new(first, true),
            new(second, false)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => new Question("Test question", options));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_UniqueOptions_CreatesQuestion()
    {
        var options = new List<AnswerOption>
        {
            new("Yes", true),
            new("No", false),
            new("Maybe", false)
        };

        var question = new Question("Test question", options);

        Assert.Equal(3, question.Options.Count);
    }
}