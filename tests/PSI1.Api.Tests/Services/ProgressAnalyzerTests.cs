using PSI1.Api.Models;
using PSI1.Api.Services;

namespace PSI1.Api.Tests.Services;

public class ProgressAnalyzerTests
{
    private readonly ProgressAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_NoAnswers_ReturnsEveryCategoryWithZero()
    {
        var result = _analyzer.Analyze([]);

        Assert.Equal(Enum.GetValues<Category>().Length, result.Categories.Count);
        Assert.All(result.Categories, c =>
        {
            Assert.Equal(0, c.Answered);
            Assert.Equal(0, c.Correct);
            Assert.Null(c.Accuracy);
            Assert.False(c.HasEnoughData);
        });

        Assert.Null(result.Strongest);
        Assert.Null(result.Weakest);
    }

    [Fact]
    public void Analyze_FewerThanMinAnswered_NotEnoughData()
    {
        var result = _analyzer.Analyze([new CategoryAnswerCounts(Category.Food, 4, 3)]);

        var food = result.Categories.Single(c => c.Category == Category.Food);
        Assert.Equal(0.75, food.Accuracy);
        Assert.False(food.HasEnoughData);
    }

    [Fact]
    public void Analyze_OneCategoryWithEnoughData_NoWeakestOrStrongest()
    {
        var result = _analyzer.Analyze([new CategoryAnswerCounts(Category.Food, 10, 7)]);

        Assert.True(result.Categories.Single(c => c.Category == Category.Food).HasEnoughData);
        Assert.Null(result.Strongest);
        Assert.Null(result.Weakest);
    }

    [Fact]
    public void Analyze_SeveralCategoriesWithEnoughData_ReturnsWeakestAndStrongest()
    {
        var result = _analyzer.Analyze(
        [
            new CategoryAnswerCounts(Category.Food, 20, 17),
            new CategoryAnswerCounts(Category.Family, 12, 6),
            new CategoryAnswerCounts(Category.Travel, 10, 8),
            new CategoryAnswerCounts(Category.Nature, 3, 0)
        ]);

        Assert.Equal(Category.Family, result.Weakest);
        Assert.Equal(Category.Food, result.Strongest);
    }
}