using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using PSI1.Api.Models;

namespace PSI1.Api.Services;

// Creates the JWT an user receives after logging in
public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(
        IConfiguration configuration,
        JwtSecurityTokenHandler tokenHandler,
        TimeProvider timeProvider)
    {
        _configuration = configuration;
        _tokenHandler = tokenHandler;
        _timeProvider = timeProvider;
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)

        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: _timeProvider.GetUtcNow().UtcDateTime.AddHours(1),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }
}