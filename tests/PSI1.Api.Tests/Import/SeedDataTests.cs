using PSI1.Api.Import;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Import;

//checks the quiz files in src/PSI1.Api/SeedData
public class SeedDataTests
{
    private static readonly string SeedDataDirectory = Path.Combine(AppContext.BaseDirectory, "SeedData");

    private static string[] GetSeedFiles() => Directory.GetFiles(SeedDataDirectory, "*.json");

    //imports every seed file
    private static async Task<List<(string FileName, QuizImportResult Result)>> ImportAllAsync()
    {
        var importer = new JsonQuizImporter();
        var results = new List<(string FileName, QuizImportResult Result)>();

        foreach (var path in GetSeedFiles())
        {
            await using var stream = File.OpenRead(path);
            results.Add((Path.GetFileName(path), await importer.ImportAsync(stream)));
        }

        return results;
    }

    private static List<string> DescribeFailures(IEnumerable<(string FileName, QuizImportResult Result)> results) =>
        results
            .Where(r => !r.Result.IsSuccess)
            .Select(r => $"{r.FileName}: {string.Join(" ", r.Result.Errors.Select(e => e.Message))}")
            .ToList();

    private static async Task<List<Quiz>> ImportAllQuizzesAsync()
    {
        var results = await ImportAllAsync();

        return results
            .Where(r => r.Result.IsSuccess)
            .Select(r => r.Result.Quiz!)
            .ToList();
    }

    [Fact]
    public void SeedFiles_Count_IsBetween30And50()
    {
        Assert.InRange(GetSeedFiles().Length, 30, 50);
    }

    [Fact]
    public async Task ImportAsync_EverySeedFile_ReturnsNoErrors()
    {
        var results = await ImportAllAsync();

        Assert.NotEmpty(results);
        Assert.Empty(DescribeFailures(results));
    }

    [Fact]
    public async Task SeedFiles_EveryCategory_IsUsed()
    {
        var quizzes = await ImportAllQuizzesAsync();

        var unused = Enum.GetValues<Category>().Except(quizzes.Select(q => q.Category));

        Assert.Empty(unused);
    }

    [Fact]
    public async Task SeedFiles_EveryLanguage_IsUsed()
    {
        var quizzes = await ImportAllQuizzesAsync();

        var unused = Enum.GetValues<Language>().Except(quizzes.Select(q => q.Language));

        Assert.Empty(unused);
    }

    [Fact]
    public async Task SeedFiles_QuestionCounts_AreBetween3And20AndVary()
    {
        var quizzes = await ImportAllQuizzesAsync();

        Assert.All(quizzes, quiz => Assert.InRange(quiz.Questions.Count, 3, 20));

        var differentCounts = quizzes.Select(q => q.Questions.Count).Distinct().Count();
        Assert.True(differentCounts >= 5, $"Expected at least 5 different question counts, found {differentCounts}.");
    }

    [Fact]
    public async Task SeedFiles_Titles_AreUnique()
    {
        var quizzes = await ImportAllQuizzesAsync();

        var duplicates = quizzes
            .GroupBy(q => q.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public async Task SeedFiles_Descriptions_AreOptional()
    {
        var quizzes = await ImportAllQuizzesAsync();

        Assert.Contains(quizzes, quiz => quiz.Description is null);
        Assert.Contains(quizzes, quiz => quiz.Description is not null);
    }
}