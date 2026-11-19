using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Configuration;

using PSI1.Api.Models;
using PSI1.Api.Services;

namespace PSI1.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "test-key-at-least-32-characters-long!!"
            })
            .Build();

        return new JwtTokenService(configuration, new JwtSecurityTokenHandler());
    }

    [Fact]
    public void CreateToken_ContainsUserIdEmailIssuerAndAudience()
    {
        var user = new User { Id = 5, Email = "user@test.com", Username = "tester" };

        var tokenText = CreateService().CreateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.Equal("5", token.Subject);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@test.com");
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
    }
}