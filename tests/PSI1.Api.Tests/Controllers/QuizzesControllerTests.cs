using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Controllers;

public class QuizzesControllerTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public QuizzesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedAsync(string title, Category category, Language language)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var questions = Enumerable.Range(0, 3)
            .Select(i => new Question($"Q{i}", [new AnswerOption("A", true), new AnswerOption("B", false)]))
            .ToList();

        db.Quizzes.Add(new Quiz(title, category, language, questions));
        await db.SaveChangesAsync();
    }

    private async Task<Seeded> SeedMatrixAsync()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var seeded = new Seeded(
            FoodItalian: $"FoodItalian-{tag}",
            FoodLithuanian: $"FoodLithuanian-{tag}",
            TravelItalian: $"TravelItalian-{tag}",
            TravelLithuanian: $"TravelLithuanian-{tag}");

        await SeedAsync(seeded.FoodItalian, Category.Food, Language.Italian);
        await SeedAsync(seeded.FoodLithuanian, Category.Food, Language.Lithuanian);
        await SeedAsync(seeded.TravelItalian, Category.Travel, Language.Italian);
        await SeedAsync(seeded.TravelLithuanian, Category.Travel, Language.Lithuanian);
        return seeded;
    }

    private record Seeded(string FoodItalian, string FoodLithuanian, string TravelItalian, string TravelLithuanian);

    private async Task<List<QuizSummaryResponse>> GetQuizzesAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<QuizSummaryResponse>>(Json))!;
    }

    [Fact]
    public async Task GetQuizzes_WithoutToken_ReturnsOk()
    {
        var response = await _client.GetAsync("api/quizzes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzes_NoFilters_ReturnsAllQuizzes()
    {
        var seeded = await SeedMatrixAsync();

        var quizzes = await GetQuizzesAsync("api/quizzes");

        var titles = quizzes.Select(q => q.Title).ToList();
        Assert.Contains(seeded.FoodItalian, titles);
        Assert.Contains(seeded.FoodLithuanian, titles);
        Assert.Contains(seeded.TravelItalian, titles);
        Assert.Contains(seeded.TravelLithuanian, titles);
    }

    [Fact]
    public async Task GetQuizzes_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var seeded = await SeedMatrixAsync();

        var quizzes = await GetQuizzesAsync("api/quizzes?category=Food");

        Assert.All(quizzes, q => Assert.Equal(Category.Food, q.Category));
        var titles = quizzes.Select(q => q.Title).ToList();
        Assert.Contains(seeded.FoodItalian, titles);
        Assert.Contains(seeded.FoodLithuanian, titles);
        Assert.DoesNotContain(seeded.TravelItalian, titles);
        Assert.DoesNotContain(seeded.TravelLithuanian, titles);
    }

    [Fact]
    public async Task GetQuizzes_FilterByLanguage_ReturnsOnlyThatLanguage()
    {
        var seeded = await SeedMatrixAsync();

        var quizzes = await GetQuizzesAsync("api/quizzes?language=Italian");

        Assert.All(quizzes, q => Assert.Equal(Language.Italian, q.Language));
        var titles = quizzes.Select(q => q.Title).ToList();
        Assert.Contains(seeded.FoodItalian, titles);
        Assert.Contains(seeded.TravelItalian, titles);
        Assert.DoesNotContain(seeded.FoodLithuanian, titles);
        Assert.DoesNotContain(seeded.TravelLithuanian, titles);
    }

    [Fact]
    public async Task GetQuizzes_FilterByCategoryAndLanguage_ReturnsOnlyBothMatching()
    {
        var seeded = await SeedMatrixAsync();

        var quizzes = await GetQuizzesAsync("api/quizzes?category=Food&language=Italian");

        Assert.All(quizzes, q =>
        {
            Assert.Equal(Category.Food, q.Category);
            Assert.Equal(Language.Italian, q.Language);
        });
        var titles = quizzes.Select(q => q.Title).ToList();
        Assert.Contains(seeded.FoodItalian, titles);
        Assert.DoesNotContain(seeded.FoodLithuanian, titles);
        Assert.DoesNotContain(seeded.TravelItalian, titles);
        Assert.DoesNotContain(seeded.TravelLithuanian, titles);
    }

    [Fact]
    public async Task GetQuizzes_FilterIsCaseInsensitive()
    {
        var seeded = await SeedMatrixAsync();

        var quizzes = await GetQuizzesAsync("api/quizzes?category=food");

        Assert.Contains(quizzes, q => q.Title == seeded.FoodItalian);
        Assert.All(quizzes, q => Assert.Equal(Category.Food, q.Category));
    }

    [Theory]
    [InlineData("api/quizzes?category=Movies")]
    [InlineData("api/quizzes?language=French")]
    [InlineData("api/quizzes?category=999")]
    [InlineData("api/quizzes?language=999")]
    public async Task GetQuizzes_UnknownFilterValue_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}