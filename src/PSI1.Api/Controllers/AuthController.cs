using Microsoft.AspNetCore.Mvc; // for features needed to build a web api controller
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data; // for AppDbContext
using PSI1.Api.DTOs; // for RegisterRequest class
using PSI1.Api.Models; // for User class
using PSI1.Api.Services; // for JwtTokenService

namespace PSI1.Api.Controllers;

// Tells asp.net that this class handles API
[ApiController]

// URL for end point
[Route("api/[controller]")]

// Inherits from ControllerBase core class
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokenService;

    public AuthController(AppDbContext db, JwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
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
            Username = request.Username.ToLower(),
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

        var token = _tokenService.CreateToken(user);

        return Ok(new { Token = token });
    }
}