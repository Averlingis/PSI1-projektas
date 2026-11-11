using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

    // ---------- GET api/quizzes{quizId}/questions/{index} ----------
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

    // ---------- GET api/quizzes
    private async Task SeedAsync(string title, Category category, Language language, int? ownerId = null, bool isPublic = true)
    {

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var questions = Enumerable.Range(0, 3)
            .Select(i => new Question($"Q{i}", [new AnswerOption("A", true), new AnswerOption("B", false)]))
            .ToList();

        db.Quizzes.Add(new Quiz(title, category, language, questions, ownerId: ownerId, isPublic: isPublic));
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

    private async Task<List<QuizSummaryResponse>> GetQuizzesAsync(string url, HttpClient? client = null)
    {
        var response = await (client ?? _client).GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<QuizSummaryResponse>>(Json))!;
    }

    // the user id is the "sub" claim of the client's token
    private static int GetUserId(HttpClient client)
    {
        var token = client.DefaultRequestHeaders.Authorization!.Parameter;
        return int.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsOk()
    {
        var response = await _client.GetAsync("api/quizzes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NoFilters_ReturnsAllPublicQuizzes()
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
    public async Task GetAll_FilterByCategory_ReturnsOnlyThatCategory()
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
    public async Task GetAll_FilterByLanguage_ReturnsOnlyThatLanguage()
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
    public async Task GetAll_FilterByCategoryAndLanguage_ReturnsOnlyBothMatching()
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
    public async Task GetAll_FilterIsCaseInsensitive()
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
    public async Task GetAll_UnknownFilterValue_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_PrivateQuiz_IsNotReturned()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var title = $"Private-{Guid.NewGuid():N}";
        await SeedAsync(title, Category.Food, Language.Italian, GetUserId(client), isPublic: false);

        var quizzes = await GetQuizzesAsync("api/quizzes");

        Assert.DoesNotContain(quizzes, q => q.Title == title);
    }

    [Fact]
    public async Task GetAll_PublicQuizWithOwner_IsReturned()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var title = $"Public-{Guid.NewGuid():N}";
        await SeedAsync(title, Category.Food, Language.Italian, GetUserId(client), isPublic: true);

        var quizzes = await GetQuizzesAsync("api/quizzes");

        Assert.Contains(quizzes, q => q.Title == title && q.IsPublic);
    }

    // ---------- GET api/quizzes/mine ----------

    [Fact]
    public async Task GetMyQuizzes_PublicAndPrivateQuizzes_ReturnsBoth()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var publicTitle = $"Public-{Guid.NewGuid():N}";
        var privateTitle = $"Private-{Guid.NewGuid():N}";

        await SeedAsync(publicTitle, Category.Food, Language.Italian, GetUserId(client), isPublic: true);
        await SeedAsync(privateTitle, Category.Food, Language.Italian, GetUserId(client), isPublic: false);

        var quizzes = await GetQuizzesAsync("api/quizzes/mine", client);

        Assert.Equal(2, quizzes.Count);
        Assert.Contains(quizzes, q => q.Title == publicTitle && q.IsPublic);
        Assert.Contains(quizzes, q => q.Title == privateTitle && !q.IsPublic);
    }

    [Fact]
    public async Task GetMyQuizzes_OtherUsersAndSeedQuizzes_AreNotReturned()
    {
        using var client1 = await _factory.CreateAuthenticatedClientAsync();
        using var client2 = await _factory.CreateAuthenticatedClientAsync();

        var myTitle = $"Mine-{Guid.NewGuid():N}";

        await SeedAsync(myTitle, Category.Food, Language.Italian, GetUserId(client1));
        await SeedAsync($"Other-{Guid.NewGuid():N}", Category.Food, Language.Italian, GetUserId(client2));

        // a quiz with no owner, like the seed quizzes
        await SeedAsync($"Seed-{Guid.NewGuid():N}", Category.Food, Language.Italian);

        var quizzes = await GetQuizzesAsync("api/quizzes/mine", client1);

        var quiz = Assert.Single(quizzes);
        Assert.Equal(myTitle, quiz.Title);
    }

    [Fact]
    public async Task GetMyQuizzes_CategoryFilter_ReturnsMatchingQuizzes()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var foodTitle = $"Food-{Guid.NewGuid():N}";

        await SeedAsync(foodTitle, Category.Food, Language.Italian, GetUserId(client));
        await SeedAsync($"Travel-{Guid.NewGuid():N}", Category.Travel, Language.Italian, GetUserId(client));

        var quizzes = await GetQuizzesAsync("api/quizzes/mine?category=Food", client);

        var quiz = Assert.Single(quizzes);
        Assert.Equal(foodTitle, quiz.Title);
    }

    [Fact]
    public async Task GetMyQuizzes_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/quizzes/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- POST api/quizzes/{quizId}/questions/{index}/answer ----------

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

    // ---------- GET api/quizzes/{quizId}/score
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

    // ---------- DELETE /api/quizzes/{quizId}/score ----------

    [Fact]
    public async Task ResetScore_AnsweredQuestion_CanBeAnsweredAgain()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response1 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        response1.EnsureSuccessStatusCode();

        var response2 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var deleteResponse = await client.DeleteAsync($"api/quizzes/{quizId}/score");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var scoreResponse = await client.GetAsync($"api/quizzes/{quizId}/score");
        Assert.Equal(HttpStatusCode.NotFound, scoreResponse.StatusCode);

        var response3 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }

    [Fact]
    public async Task ResetScore_NoScore_ReturnsNoContent()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var deleteResponse = await client.DeleteAsync($"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ResetScore_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();
        var deleteResponse = await _client.DeleteAsync($"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ResetScore_OtherUsersScore_IsKept()
    {
        var quizId = await CreateTestQuizAsync();
        using var client1 = await _factory.CreateAuthenticatedClientAsync();
        using var client2 = await _factory.CreateAuthenticatedClientAsync();

        var answer1 = await client1.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        var answer2 = await client2.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        Assert.Equal(HttpStatusCode.OK, answer1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, answer2.StatusCode);

        var deleteResponse = await client1.DeleteAsync($"api/quizzes/{quizId}/score");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var score1 = await client1.GetAsync($"api/quizzes/{quizId}/score");
        var score2 = await client2.GetAsync($"api/quizzes/{quizId}/score");

        Assert.Equal(HttpStatusCode.NotFound, score1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, score2.StatusCode);
    }

    [Fact]
    public async Task ResetScore_FinishedQuiz_StartsNewScore()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var answer1 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        var answer2 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/1/answer", new AnswerRequest(1));
        var answer3 = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/2/answer", new AnswerRequest(2));

        Assert.Equal(HttpStatusCode.OK, answer1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, answer2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, answer3.StatusCode);

        var deleteResponse = await client.DeleteAsync($"api/quizzes/{quizId}/score");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var replay = await client.PostAsJsonAsync($"api/quizzes/{quizId}/questions/0/answer", new AnswerRequest(0));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);

        var scoreResponse = await client.GetAsync($"api/quizzes/{quizId}/score");
        Assert.Equal(HttpStatusCode.OK, scoreResponse.StatusCode);

        var score = await scoreResponse.Content.ReadFromJsonAsync<QuizScoreResponse>();
        Assert.NotNull(score);
        Assert.Equal(1, score.TotalAnswered);
        Assert.Equal(0, score.CorrectAnswers);
    }
    // ---------- GET api/quizzes/{quizId}/progress ----------

    private static async Task AnswerQuestionsAsync(HttpClient client, int quizId, params int[] questionIndexes)
    {
        foreach (var index in questionIndexes)
        {
            var response = await client.PostAsJsonAsync(
                $"api/quizzes/{quizId}/questions/{index}/answer",
                new AnswerRequest(0));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<QuizProgressResponse> GetProgressAsync(HttpClient client, int quizId)
    {
        var response = await client.GetAsync($"api/quizzes/{quizId}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var progress = await response.Content.ReadFromJsonAsync<QuizProgressResponse>();

        Assert.NotNull(progress);
        return progress;
    }

    [Fact]
    public async Task GetProgress_NotStarted_ReturnsFirstQuestion()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var progress = await GetProgressAsync(client, quizId);

        Assert.Equal(0, progress.NextQuestionIndex);
        Assert.Equal(0, progress.AnsweredCount);
        Assert.Equal(3, progress.TotalQuestions);
    }

    [Fact]
    public async Task GetProgress_PartlyAnswered_ReturnsNextQuestion()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();
        await AnswerQuestionsAsync(client, quizId, 0, 1);

        var progress = await GetProgressAsync(client, quizId);

        Assert.Equal(2, progress.NextQuestionIndex);
        Assert.Equal(2, progress.AnsweredCount);
        Assert.Equal(3, progress.TotalQuestions);
    }

    [Fact]
    public async Task GetProgress_AnsweredOutOfOrder_ReturnsLowestUnansweredQuestion()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();
        //question 2 first, then question 0: question 1 is still open
        await AnswerQuestionsAsync(client, quizId, 2, 0);

        var progress = await GetProgressAsync(client, quizId);

        Assert.Equal(1, progress.NextQuestionIndex);
        Assert.Equal(2, progress.AnsweredCount);
        Assert.Equal(3, progress.TotalQuestions);
    }

    [Fact]
    public async Task GetProgress_Completed_ReturnsNullNextQuestion()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();
        await AnswerQuestionsAsync(client, quizId, 0, 1, 2);

        var response = await client.GetAsync($"api/quizzes/{quizId}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //read the raw JSON, nextQuestionIndex must be present and null, not left out
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextQuestionIndex").ValueKind);
        Assert.Equal(3, root.GetProperty("answeredCount").GetInt32());
        Assert.Equal(3, root.GetProperty("totalQuestions").GetInt32());
    }

    [Fact]
    public async Task GetProgress_NonexistentQuiz_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("api/quizzes/2147483647/progress");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProgress_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();

        var response = await _client.GetAsync($"api/quizzes/{quizId}/progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProgress_OtherUsersAnswers_AreNotCounted()
    {
        var quizId = await CreateTestQuizAsync();
        using var client1 = await _factory.CreateAuthenticatedClientAsync();
        using var client2 = await _factory.CreateAuthenticatedClientAsync();
        await AnswerQuestionsAsync(client1, quizId, 0, 1);

        var progress = await GetProgressAsync(client2, quizId);

        Assert.Equal(0, progress.NextQuestionIndex);
        Assert.Equal(0, progress.AnsweredCount);
    }

    [Fact]
    public async Task GetProgress_AnswersInOtherQuiz_AreNotCounted()
    {
        var answeredQuizId = await CreateTestQuizAsync();
        var otherQuizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();
        await AnswerQuestionsAsync(client, answeredQuizId, 0, 1);

        var progress = await GetProgressAsync(client, otherQuizId);

        Assert.Equal(0, progress.NextQuestionIndex);
        Assert.Equal(0, progress.AnsweredCount);
    }

    // ---------- GET api/quizzes/in-progress/latest ----------

    [Fact]
    public async Task GetLatestInProgress_NoQuizzesStarted_ReturnsNoContent()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("api/quizzes/in-progress/latest");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

   [Fact]
    public async Task GetLatestInProgress_OneUnfinishedQuiz_ReturnsQuizIdAndTitle()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var quizId = await CreateTestQuizAsync();
        await AnswerQuestionsAsync(client, quizId, 0);

        var response = await client.GetAsync("api/quizzes/in-progress/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var latest = await response.Content.ReadFromJsonAsync<QuizInProgressResponse>();

        Assert.NotNull(latest);
        Assert.Equal(quizId, latest.QuizId);
        Assert.Equal("Family Vocabulary", latest.Title);
    }

   [Fact]
    public async Task GetLatestInProgress_SeveralUnfinishedQuizzes_ReturnsLatest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var olderQuizId = await CreateTestQuizAsync();
        var newerQuizId = await CreateTestQuizAsync();

        await AnswerQuestionsAsync(client, olderQuizId, 0);
        await AnswerQuestionsAsync(client, newerQuizId, 0);
        await AnswerQuestionsAsync(client, olderQuizId, 1);

        var response = await client.GetAsync("api/quizzes/in-progress/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var latest = await response.Content.ReadFromJsonAsync<QuizInProgressResponse>();

        Assert.NotNull(latest);
        Assert.Equal(olderQuizId, latest.QuizId);
    }

    [Fact]
    public async Task GetLatestInProgress_CompletedQuiz_IsIgnored()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var quizId = await CreateTestQuizAsync();
        await AnswerQuestionsAsync(client, quizId, 0, 1, 2);

        var response = await client.GetAsync("api/quizzes/in-progress/latest");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestInProgress_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/quizzes/in-progress/latest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- POST api/quizzes/import ----------

    private static MultipartFormDataContent ToFormContent(string json)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(json), "file", "quiz.json");
        return content;
    }

    [Fact]
    public async Task ImportQuiz_WithoutToken_ReturnsUnauthorized()
    {
        var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("api/quizzes/import", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ImportQuiz_ValidFile_ReturnsCreatedAndSavesQuiz()
    {
        var json = """
        {
            "title": "Imported Quiz",
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
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("api/quizzes/import", ToFormContent(json));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quizzes = await client.GetFromJsonAsync<List<QuizSummaryResponse>>("api/quizzes", Json);
        Assert.Contains(quizzes!, q => q.Title == "Imported Quiz");
    }

    [Fact]
    public async Task ImportQuiz_MalformedJson_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("api/quizzes/import", ToFormContent("{ \"title\": "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportQuiz_QuestionWithoutText_ReturnsBadRequestWithQuestionNumber()
    {
        var json = """
            {
                "title": "Family Vocabulary",
                "description": "Basic family words in Lithuanian",
                "category": "Family",
                "language": "Lithuanian",
                "questions": [
                {
                    "text": "How do you say 'Mother' in Lithuanian?",
                    "options": [
                    { "text": "Sesuo", "isCorrect": false },
                    { "text": "Mama", "isCorrect": true },
                    { "text": "Močiutė", "isCorrect": false }
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
                    "options": [
                    { "text": "Močiutė", "isCorrect": true },
                    { "text": "Teta", "isCorrect": false }
                    ]
                }
                ]
            }
            """;

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("api/quizzes/import", ToFormContent(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"location\":3", body);
    }

    [Fact]
    public async Task ImportQuiz_DuplicateQuiz_ReturnsConflict()
    {
        var json = """
        {
            "title": "Duplicate Quiz",
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

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response1 = await client.PostAsync("api/quizzes/import", ToFormContent(json));
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var response2 = await client.PostAsync("api/quizzes/import", ToFormContent(json));
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task ImportQuiz_NoFile_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("api/quizzes/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}