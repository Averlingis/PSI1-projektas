using System.Net;
using System.Net.Http.Json;


namespace PSI1.Api.Tests.Controllers;

public class LanguagesControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public LanguagesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------- GET /api/languages ----------

    [Fact]
    public async Task GetLanguages_WithoutToken_ReturnsAllLanguages()
    {
        var response = await _client.GetAsync("api/languages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var languages = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.Equal(new[] { "Lithuanian", "Russian", "Italian" }, languages);
    }

    // ---------- PUT /api/languages/select ----------

    [Fact]
    public async Task SelectLanguage_ValidToken_ReturnsOk()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("api/languages/select", new { language = "Lithuanian" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SelectLanguage_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/languages/select", new { language = "Lithuanian" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SelectLanguage_UnknownLanguage_ReturnsBadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("api/languages/select", new { language = "French" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /api/languages/selected ----------

    private record SelectedLanguageResponse(string? Language);

    [Fact]
    public async Task GetSelected_NewUser_ReturnsNullLanguage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("api/languages/selected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SelectedLanguageResponse>();
        Assert.Null(body!.Language);
    }

    [Fact]
    public async Task GetSelected_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/languages/selected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSelected_AfterSelect_ReturnsSelectedLanguage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await client.PutAsJsonAsync("api/languages/select", new { language = "Lithuanian" });

        var response = await client.GetAsync("api/languages/selected");

        var body = await response.Content.ReadFromJsonAsync<SelectedLanguageResponse>();
        Assert.Equal("Lithuanian", body!.Language);
    }

    [Fact]
    public async Task SelectLanguage_OneUser_DoesNotAffectAnotherUser()
    {
        var clientA = await _factory.CreateAuthenticatedClientAsync();
        var putResponse = await clientA.PutAsJsonAsync("api/languages/select", new { language = "Lithuanian" });
        putResponse.EnsureSuccessStatusCode();

        var clientB = await _factory.CreateAuthenticatedClientAsync();

        var response = await clientB.GetAsync("api/languages/selected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SelectedLanguageResponse>();
        Assert.Null(body!.Language);
    }
}