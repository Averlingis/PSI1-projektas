using System.Net;
using System.Net.Http.Json;


namespace PSI1.Api.Tests.Controllers;

public class CategoriesControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public CategoriesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------- GET /api/categories ----------
    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsAllCategories()
    {
        var response = await _client.GetAsync("api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.Equal(
        new[]
        {
            "Nature", "Culture", "Food", "Travel", "Work", "Health",
            "Home", "Shopping", "Education", "Technology", "Sports", "Family"
        },
        categories);
    }

    // ---------- PUT /api/categories/select ----------

    [Fact]
    public async Task SelectCategory_ValidToken_ReturnsOk()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("api/categories/select", new { category = "Food" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SelectCategory_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/categories/select", new { category = "Work" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SelectCategory_UnknownCategory_ReturnsBadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("api/categories/select", new { category = "Movies" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /api/categories/selected ----------

    private record SelectedCategoryResponse(string? Category);

    [Fact]
    public async Task GetSelected_NewUser_ReturnsNullCategory()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("api/categories/selected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SelectedCategoryResponse>();
        Assert.Null(body!.Category);
    }

    [Fact]
    public async Task GetSelected_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/categories/selected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSelected_AfterSelect_ReturnsSelectedCategory()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await client.PutAsJsonAsync("api/categories/select", new { category = "Food" });

        var response = await client.GetAsync("api/categories/selected");

        var body = await response.Content.ReadFromJsonAsync<SelectedCategoryResponse>();
        Assert.Equal("Food", body!.Category);
    }

    [Fact]
    public async Task SelectCategory_OneUser_DoesNotAffectAnotherUser()
    {
        var clientA = await _factory.CreateAuthenticatedClientAsync();
        var putResponse = await clientA.PutAsJsonAsync("api/categories/select", new { category = "Work" });
        putResponse.EnsureSuccessStatusCode();

        var clientB = await _factory.CreateAuthenticatedClientAsync();

        var response = await clientB.GetAsync("api/categories/selected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SelectedCategoryResponse>();
        Assert.Null(body!.Category);
    }
}