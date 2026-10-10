using System.Net;
using System.Net.Http.Json;

using PSI1.Api.DTOs;

namespace PSI1.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }


    // ---------- Register ----------

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        var request = new RegisterRequest(
            $"{Guid.NewGuid():N}@test.com",
            Guid.NewGuid().ToString("N")[..12],
            "password123");

        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var first = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        var second = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        await _client.PostAsJsonAsync("api/auth/register", first);

        var response = await _client.PostAsJsonAsync("api/auth/register", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmailDifferentCase_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var first = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        var second = new RegisterRequest(email.ToUpper(), Guid.NewGuid().ToString("N")[..12], "password123");
        await _client.PostAsJsonAsync("api/auth/register", first);

        var response = await _client.PostAsJsonAsync("api/auth/register", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var username = Guid.NewGuid().ToString("N")[..12];
        var first = new RegisterRequest($"{Guid.NewGuid():N}@test.com", username, "password123");
        var second = new RegisterRequest($"{Guid.NewGuid():N}@test.com", username, "password123");
        await _client.PostAsJsonAsync("api/auth/register", first);

        var response = await _client.PostAsJsonAsync("api/auth/register", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsernameDifferentCase_ReturnsConflict()
    {
        var username = Guid.NewGuid().ToString("N")[..12];
        var first = new RegisterRequest($"{Guid.NewGuid():N}@test.com", username, "password123");
        var second = new RegisterRequest($"{Guid.NewGuid():N}@test.com", username.ToUpper(), "password123");
        await _client.PostAsJsonAsync("api/auth/register", first);

        var response = await _client.PostAsJsonAsync("api/auth/register", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest(
            Guid.NewGuid().ToString("N")[..20],
            Guid.NewGuid().ToString("N")[..12],
            "password123");

        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var request = new RegisterRequest(
            $"{Guid.NewGuid():N}@test.com",
            Guid.NewGuid().ToString("N")[..12],
            "123");

        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("has space")]
    public async Task Register_InvalidUsername_ReturnsBadRequest(string username)
    {
        var request = new RegisterRequest($"{Guid.NewGuid():N}@test.com", username, "password123");

        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login ----------

    private record LoginResponse(string Token);

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var register = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        await _client.PostAsJsonAsync("api/auth/register", register);

        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.Token));
    }

    [Fact]
    public async Task Login_EmailDifferentCase_ReturnsOkWithToken()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var register = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        await _client.PostAsJsonAsync("api/auth/register", register);

        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email.ToUpper(), "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var register = new RegisterRequest(email, Guid.NewGuid().ToString("N")[..12], "password123");
        await _client.PostAsJsonAsync("api/auth/register", register);

        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "password345"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest($"{Guid.NewGuid():N}@test.com", "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}