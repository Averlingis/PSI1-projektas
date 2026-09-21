using PSI1.Api.Data; // for AppDbContext
using PSI1.Api.Models; // for User class
using PSI1.Api.DTOs; // for RegisterRequest class
using Microsoft.AspNetCore.Mvc; // for features needed to build a web api controller
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PSI1.Api.Controllers;

// Tells asp.net that this class handles API
[ApiController]

// URL for end point
[Route("api/[controller]")]

// Inhertis from ControllerBase core class
public class AuthController : ControllerBase
{
	// where user data is saved on disk, value is shared across all requests and never changes for now
	private readonly AppDbContext _db;
	private readonly IConfiguration _configuration;

	public AuthController(AppDbContext db, IConfiguration configuration)
	{
		_db = db;
		_configuration = configuration;
	}

	// Handles POST req
	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterRequest request)
	{
		// reject duplicate emails instead of silently adding
		var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
		if (emailExists)
		{
			return Conflict(new { message = "Email already registered." });
		}

		// reject duplicate usernames too, since Username is unique in the database
		var usernameExists = await _db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
		if (usernameExists)
		{
			return Conflict(new { message = "Username already taken." });
		}

		var user = new User
		{
			Email = request.Email.ToLower(), // store emails in lowercase to avoid duplicates
			Username = request.Username,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) // bcrypt hashes the password and generates + embeds a random salt for us
		};

		_db.Users.Add(user);
		await _db.SaveChangesAsync();

		return StatusCode(201, new { message = "Account created." });
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginRequest request)
	{
		var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

		if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
		{
			return Unauthorized(new { message = "Invalid email or password." });
		}

		var token = GenerateJwtToken(user);

		return Ok(new { Token = token });
	}

	private string GenerateJwtToken(User user)
	{
		var claims = new[]
		{
		new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
		new Claim(JwtRegisteredClaimNames.Email, user.Email),
		new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
	    };

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
		    issuer: _configuration["Jwt:Issuer"],
		    audience: _configuration["Jwt:Audience"],
		    claims: claims,
		    expires: DateTime.UtcNow.AddHours(1),
		    signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}

