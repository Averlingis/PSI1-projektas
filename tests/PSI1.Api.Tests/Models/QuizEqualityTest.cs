using PSI1.Api.Models;

namespace PSI1.Api.Tests.Models;

public class QuizEqualityTests
{
    private static List<Question> CreateQuestions() =>
        Enumerable.Range(1, Quiz.MinQuestions)
            .Select(i => new Question($"Question {i}",
            [
                new AnswerOption("Right", true),
                new AnswerOption("Wrong", false)
            ]))
            .ToList();

    private static Quiz CreateQuiz(
        string title = "Food Basics",
        Category category = Category.Food,
        Language language = Language.Lithuanian,
        string? description = null) =>
        new(title, category, language, CreateQuestions(), description);

    [Fact]
    public void Equals_SameTitleCategoryLanguage_ReturnsTrue()
    {
        Assert.True(CreateQuiz().Equals(CreateQuiz()));
    }

    [Fact]
    public void Equals_TitleDiffersByCase_ReturnsTrue()
    {
        Assert.True(CreateQuiz("food basics").Equals(CreateQuiz("FOOD BASICS")));
    }

    [Fact]
    public void Equals_TitleDiffersByWhitespace_ReturnsTrue()
    {
        Assert.True(CreateQuiz("  Food Basics ").Equals(CreateQuiz("Food Basics")));
    }

    [Fact]
    public void Equals_DifferentTitle_ReturnsFalse()
    {
        Assert.False(CreateQuiz("Food Basics").Equals(CreateQuiz("Food Advanced")));
    }

    [Fact]
    public void Equals_DifferentCategory_ReturnsFalse()
    {
        Assert.False(CreateQuiz(category: Category.Food).Equals(CreateQuiz(category: Category.Travel)));
    }

    [Fact]
    public void Equals_DifferentLanguage_ReturnsFalse()
    {
        Assert.False(CreateQuiz(language: Language.Italian).Equals(CreateQuiz(language: Language.Russian)));
    }

    [Fact]
    public void Equals_DifferentDescription_StillEqual()
    {
        Assert.True(CreateQuiz(description: "a").Equals(CreateQuiz(description: "b")));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.False(CreateQuiz().Equals(null));
    }

    [Fact]
    public void Equals_Object_OtherType_ReturnsFalse()
    {
        Assert.False(CreateQuiz().Equals("Food Basics"));
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var quiz = CreateQuiz();
        Assert.True(quiz.Equals(quiz));
    }

    [Fact]
    public void GetHashCode_EqualQuizzes_HaveSameHash()
    {
        Assert.Equal(CreateQuiz("Food Basics").GetHashCode(), CreateQuiz("  FOOD BASICS ").GetHashCode());
    }

    [Fact]
    public void ListContains_DetectsDuplicate()
    {
        var existing = new List<Quiz> { CreateQuiz("Food Basics") };

        Assert.Contains(CreateQuiz(" food basics "), existing);
        Assert.DoesNotContain(CreateQuiz("Other"), existing);
    }

    [Fact]
    public void HashSet_TreatsEqualQuizzesAsDuplicates()
    {
        var set = new HashSet<Quiz> { CreateQuiz("Food Basics") };

        Assert.False(set.Add(CreateQuiz("FOOD BASICS")));
    }
}