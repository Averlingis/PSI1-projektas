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
        Assert.True(question.QuestionId > 0);
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
}