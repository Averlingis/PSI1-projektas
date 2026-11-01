using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PSI1.Api.Data;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Data;

public class QuizSeederTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly string SeedDataDirectory = Path.Combine(AppContext.BaseDirectory, "SeedData");

    private const string ValidQuizJson = """
    {
      "title": "Family Vocabulary",
      "category": "Family",
      "language": "Lithuanian",
      "questions": [
        {
          "text": "How do you say 'Mother' in Lithuanian?",
          "options": [
            { "text": "Sesuo", "isCorrect": false },
            { "text": "Mama", "isCorrect": true }
          ]
        },
        {
          "text": "What does 'Brolis' mean in English?",
          "options": [
            { "text": "Father", "isCorrect": false },
            { "text": "Brother", "isCorrect": true }
          ]
        },
        {
          "text": "How do you say 'Grandmother' in Lithuanian?",
          "options": [
            { "text": "Močiutė", "isCorrect": true },
            { "text": "Teta", "isCorrect": false }
          ]
        }
      ]
    }
    """;

    private readonly ApiFactory _factory;

    public QuizSeederTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // runs before every test: the database is shared inside this class, so start from empty
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Quizzes.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Quiz CreateQuiz()
    {
        var questions = Enumerable.Range(1, 3)
            .Select(i => new Question($"Question {i}?", new List<AnswerOption>
            {
                new("Right", true),
                new("Wrong", false)
            }))
            .ToList();

        return new Quiz("Existing quiz", Category.Family, Language.Lithuanian, questions);
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_ImportsEverySeedFile()
    {
        var fileCount = Directory.GetFiles(SeedDataDirectory, "*.json").Length;

        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<QuizSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await seeder.SeedAsync(SeedDataDirectory);

        Assert.Equal(fileCount, await db.Quizzes.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DatabaseWithQuiz_LeavesItUnchanged()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<QuizSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Quizzes.Add(CreateQuiz());
        await db.SaveChangesAsync();

        await seeder.SeedAsync(SeedDataDirectory);

        Assert.Equal(1, await db.Quizzes.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_InvalidFile_ThrowsAndSavesNothing()
    {
        // one valid and one broken file: the valid quiz must not be saved either
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "valid.json"), ValidQuizJson);
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "broken.json"), "{}");

            using var scope = _factory.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<QuizSeeder>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => seeder.SeedAsync(directory.FullName));

            Assert.Contains("broken.json", exception.Message);
            Assert.Equal(0, await db.Quizzes.CountAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}