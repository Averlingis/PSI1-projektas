using PSI1.Api.Data; // for AppDbContext
using Microsoft.AspNetCore.Mvc; // for features needed to build a web api controller
using PSI1.Api.Models; // for User class
using Microsoft.EntityFrameworkCore; // for async queries

namespace PSI1.Api.Controllers;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);


// Tells asp.net that this class handles API
[ApiController]

// URL for end point
[Route("api/[controller]")]

// Inhertis from ControllerBase core class
public class AuthController : ControllerBase
{
	// where user data is saved on disk, value is shared across all requests and never changes for now
	private readonly AppDbContext _db;

	public AuthController(AppDbContext db)
	{
		_db = db;
	}

	// Handles POST req
	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterRequest request)
	{
		// reject duplicate usernames instead of silently adding
		var exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
		if (exists)
		{
			return Conflict("Username already taken.");
		}

		var user = new User
		{
			Username = request.Username,
			// bcrypt hashes the password and generates + embeds a random salt for us
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
		};

		_db.Users.Add(user);
		await _db.SaveChangesAsync();

		return Ok("Account created.");
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginRequest request)
	{
		var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

		if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
		{
			return Unauthorized("Wrong username or password.");
		}

		return Ok("Logged in.");
	}
}