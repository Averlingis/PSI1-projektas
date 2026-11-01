using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using PSI1.Api.Exceptions;
using PSI1.Api.Extensions;

namespace PSI1.Api.Tests.Extensions;

public class UserLookupExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(string claimType, string value) =>
        new(new ClaimsIdentity(new[] { new Claim(claimType, value) }));

    [Fact]
    public void GetUserId_ValidSub_ReturnsId()
    {
        var principal = CreatePrincipal(JwtRegisteredClaimNames.Sub, "42");

        Assert.Equal(42, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ValidNameIdentifier_ReturnsId()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "42");

        Assert.Equal(42, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_NoClaim_Throws()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidTokenException>(() => principal.GetUserId());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("4.5")]
    public void GetUserId_NonNumericSub_Throws(string value)
    {
        var principal = CreatePrincipal(JwtRegisteredClaimNames.Sub, value);

        Assert.Throws<InvalidTokenException>(() => principal.GetUserId());
    }
}