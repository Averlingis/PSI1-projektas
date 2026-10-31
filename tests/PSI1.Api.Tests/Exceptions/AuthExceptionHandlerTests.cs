using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using PSI1.Api.Data;

namespace PSI1.Api.Tests.Exceptions;

public class AuthExceptionHandlerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthExceptionHandlerTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private record ErrorResponse(string Message);

    [Fact]
    public async Task GetSelected_UserDeleted_ReturnsUnauthorizedWithMessage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.ExecuteDeleteAsync();
        }

        var response = await client.GetAsync("api/categories/selected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("User not found.", body!.Message);
    }

    [Fact]
    public async Task GetSelected_TokenWithoutSub_ReturnsUnauthorizedWithMessage()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-at-least-32-characters-long!!"));
        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: new[] { new Claim("email", "a@test.com") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        var response = await client.GetAsync("api/categories/selected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid or missing token.", body!.Message);
    }
}