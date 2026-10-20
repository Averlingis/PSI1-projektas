using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;
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

    private async Task<int> CreateTestQuizAsync()
    {
        var questions = new List<Question>
    {
        new("How do you say 'Mother' in Lithuanian?", new List<AnswerOption>
        {
            new("Sesuo", false),
            new("Mama", true),
            new("Močiutė", false)
        }),
        new("What does 'Brolis' mean in English?", new List<AnswerOption>
        {
            new("Father", false),
            new("Brother", true),
            new("Uncle", false)
        }),
        new("How do you say 'Grandmother' in Lithuanian?", new List<AnswerOption>
        {
            new("Močiutė", true),
            new("Teta", false),
            new("Dukra", false)
        })
    };

        var quiz = new Quiz(
            "Family Vocabulary",
            Category.Family,
            Language.Lithuanian,
            questions
        );

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        return quiz.Id;
    }

    [Theory]
    [InlineData(0, "How do you say 'Mother' in Lithuanian?", new string[] { "Sesuo", "Mama", "Močiutė" })]
    [InlineData(1, "What does 'Brolis' mean in English?", new string[] { "Father", "Brother", "Uncle" })]
    public async Task GetQuestion_ValidIndex_ReturnsQuestionWithoutCorrectAnswer(
        int index, string expectedText, string[] expectedOptions)
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"api/quizzes/{quizId}/questions/{index}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<QuestionPlayResponse>();

        Assert.NotNull(question);
        Assert.Equal(index, question.Position);
        Assert.Equal(expectedText, question.QuestionText);
        Assert.Equal(expectedOptions, question.Options);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isCorrect", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetQuestion_NonexistentQuiz_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "api/quizzes/2147483647/questions/0"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public async Task GetQuestion_InvalidIndex_ReturnsNotFound(int index)
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"api/quizzes/{quizId}/questions/{index}"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestion_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();

        var response = await _client.GetAsync(
            $"api/quizzes/{quizId}/questions/0"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public async Task AnswerQuestion_SelectedOption_ReturnsCorrectnessAndCorrectText(int selectedOptionIndex, bool expectedIsCorrect)
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/0/answer",
            new AnswerRequest(selectedOptionIndex));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnswerResultResponse>();

        Assert.NotNull(result);
        Assert.Equal(expectedIsCorrect, result.IsCorrect);
        Assert.Equal("Mama", result.CorrectOptionText);
    }

    [Fact]
    public async Task AnswerQuestion_TwoQuestions_UsesSingleQuizScore()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(1));
        await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/1/answer", new AnswerRequest(0));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var scores = await db.QuizScores
            .Where(s => s.QuizId == quizId)
            .ToListAsync();

        var score = Assert.Single(scores);
        Assert.Equal(2, score.Answers.Count);
        Assert.True(score.Answers[0]);
        Assert.False(score.Answers[1]);
    }

    [Fact]
    public async Task AnswerQuestion_SameQuestionTwice_ReturnsConflict()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response1 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(1));
        var response2 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task AnswerQuestion_SameQuestionTwice_KeepsFirstAnswer()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(1));
        await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var scores = await db.QuizScores
            .Where(s => s.QuizId == quizId)
            .ToListAsync();

        var score = Assert.Single(scores);
        var answer = Assert.Single(score.Answers);

        Assert.Equal(0, answer.Key);
        Assert.True(answer.Value);
    }

    [Fact]
    public async Task AnswerQuestion_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();

        var response = await _client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnswerQuestion_NonexistentQuiz_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("api/quizzes/12345678/questions/0/answer", new AnswerRequest(1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task AnswerQuestion_InvalidQuestionIndex_ReturnsNotFound(int index)
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/{index}/answer", new AnswerRequest(1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task AnswerQuestion_InvalidOptionIndex_ReturnsBadRequest(int optionIndex)
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(optionIndex));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnswerQuestion_DifferentUsers_HaveSeparateScores()
    {
        var quizId = await CreateTestQuizAsync();
        using var client1 = await _factory.CreateAuthenticatedClientAsync();
        using var client2 = await _factory.CreateAuthenticatedClientAsync();

        var response1 = await client1.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        var response2 = await client2.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var scores = await db.QuizScores
            .Where(s => s.QuizId == quizId)
            .ToListAsync();

        Assert.Equal(2, scores.Count);
        Assert.NotEqual(scores[0].UserId, scores[1].UserId);
    }

    [Fact]
    public async Task GetScore_ThreeAnswers_ReturnsCorrectCountAndTotal()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var answerResponse1 = await client.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/0/answer",
            new AnswerRequest(1));

        Assert.Equal(HttpStatusCode.OK, answerResponse1.StatusCode);

        var answerResponse2 = await client.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/1/answer",
            new AnswerRequest(0));

        Assert.Equal(HttpStatusCode.OK, answerResponse2.StatusCode);

        var answerResponse3 = await client.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/2/answer",
            new AnswerRequest(0));

        Assert.Equal(HttpStatusCode.OK, answerResponse3.StatusCode);

        var response = await client.GetAsync($"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QuizScoreResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.CorrectAnswers);
        Assert.Equal(3, result.TotalAnswered);
    }

    [Fact]
    public async Task GetScore_NoAnswers_ReturnsNotFound()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetScore_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();

        var response = await _client.GetAsync(
            $"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetScore_DifferentUsers_ReturnsOwnScore()
    {
        var quizId = await CreateTestQuizAsync();

        using var client1 = await _factory.CreateAuthenticatedClientAsync();
        using var client2 = await _factory.CreateAuthenticatedClientAsync();

        var answer1 = await client1.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/0/answer",
            new AnswerRequest(1));

        var answer2 = await client2.PostAsJsonAsync(
            $"api/quizzes/{quizId}/questions/0/answer",
            new AnswerRequest(0));

        Assert.Equal(HttpStatusCode.OK, answer1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, answer2.StatusCode);

        var response1 = await client1.GetAsync($"api/quizzes/{quizId}/score");
        var response2 = await client2.GetAsync($"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var score1 = await response1.Content.ReadFromJsonAsync<QuizScoreResponse>();
        var score2 = await response2.Content.ReadFromJsonAsync<QuizScoreResponse>();

        Assert.NotNull(score1);
        Assert.NotNull(score2);

        Assert.Equal(1, score1.CorrectAnswers);
        Assert.Equal(1, score1.TotalAnswered);

        Assert.Equal(0, score2.CorrectAnswers);
        Assert.Equal(1, score2.TotalAnswered);
    }
}