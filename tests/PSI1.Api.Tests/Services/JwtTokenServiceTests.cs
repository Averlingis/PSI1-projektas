using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using PSI1.Api.Models;
using PSI1.Api.Services;

namespace PSI1.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const string Key = "test-key-at-least-32-characters-long!!";

    private static readonly User TestUser = new() { Id = 5, Email = "user@test.com", Username = "tester" };

    private static JwtTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Key"] = Key
            })
            .Build();

        return new JwtTokenService(configuration, new JwtSecurityTokenHandler(), TimeProvider.System);
    }

    private static TokenValidationParameters ValidationParameters(string key)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    }

    [Fact]
    public void CreateToken_ContainsUserIdAndEmail()
    {
        var tokenText = CreateService().CreateToken(TestUser);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.Equal("5", token.Subject);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@test.com");
        Assert.Equal(Issuer, token.Issuer);
        Assert.Contains(Audience, token.Audiences);
    }

    [Fact]
    public void CreateToken_SignedWithConfiguredKey_PassesValidation()
    {
        var tokenText = CreateService().CreateToken(TestUser);

        new JwtSecurityTokenHandler().ValidateToken(tokenText, ValidationParameters(Key), out var validated);

        var token = Assert.IsType<JwtSecurityToken>(validated);
        Assert.Equal("5", token.Subject);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
    }

    [Fact]
    public void CreateToken_ValidatedWithDifferentKey_IsRejected()
    {
        var tokenText = CreateService().CreateToken(TestUser);
        var wrongKey = "a-completely-different-key-of-32-chars!!";

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenText, ValidationParameters(wrongKey), out _));
    }

    [Fact]
    public void CreateToken_TamperedPayload_IsRejected()
    {
        var tokenText = CreateService().CreateToken(TestUser);

        var parts = tokenText.Split('.');
        var forgedPayload = Base64UrlEncoder.Encode("""{"sub":"999"}""");
        var tampered = $"{parts[0]}.{forgedPayload}.{parts[2]}";

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tampered, ValidationParameters(Key), out _));
    }

    [Fact]
    public void CreateToken_ExpiresAfterOneHour()
    {
        var before = DateTime.UtcNow;

        var tokenText = CreateService().CreateToken(TestUser);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        var after = DateTime.UtcNow;

        Assert.InRange(token.ValidTo, before.AddHours(1).AddSeconds(-1), after.AddHours(1).AddSeconds(1));
    }
}