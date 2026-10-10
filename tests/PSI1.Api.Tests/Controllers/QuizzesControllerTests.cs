using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Controllers;

public class QuizzesControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public QuizzesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Creates sample language quiz in the test database
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

        // Saves quiz so the endpoint can retrieve it
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        return quiz.Id;
    }


    // Returns requested question without exposing the correct answer
    [Fact]
    public async Task GetQuestion_ValidIndex_ReturnsQuestionWithoutCorrectAnswer()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"api/quizzes/{quizId}/questions/0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<QuestionPlayResponse>();

        Assert.NotNull(question);
        Assert.True(question.QuestionId > 0);
        Assert.Equal(0, question.Position);
        Assert.Equal("How do you say 'Mother' in Lithuanian?", question.QuestionText);
        Assert.Equal(new[] { "Sesuo", "Mama", "Močiutė" }, question.Options);

        // The API must not reveal which answer is correct.
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isCorrect", json, StringComparison.OrdinalIgnoreCase);
    }

    // Returns correct question when requesting another position
    [Fact]
    public async Task GetQuestion_SecondIndex_ReturnsSecondQuestion()
    {
        var quizId = await CreateTestQuizAsync();
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"api/quizzes/{quizId}/questions/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<QuestionPlayResponse>();

        Assert.NotNull(question);
        Assert.Equal(1, question.Position);
        Assert.Equal("What does 'Brolis' mean in English?", question.QuestionText);
        Assert.Equal(new[] { "Father", "Brother", "Uncle" }, question.Options);
    }

    // Returns 404 when the quiz does not exist
    [Fact]
    public async Task GetQuestion_NonexistentQuiz_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "api/quizzes/2147483647/questions/0"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Returns 404 when the question index is outside quiz range
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

    // Only authenticated users can access
    [Fact]
    public async Task GetQuestion_WithoutToken_ReturnsUnauthorized()
    {
        var quizId = await CreateTestQuizAsync();

        var response = await _client.GetAsync(
            $"api/quizzes/{quizId}/questions/0"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}